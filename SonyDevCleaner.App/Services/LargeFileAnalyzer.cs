using System.IO;
using System.Security;
using SonyDevCleaner.App.Models;

namespace SonyDevCleaner.App.Services;

public sealed class LargeFileAnalyzer
{
    public Task<LargeFileAnalysisResult> AnalyzeAsync(
        string rootPath,
        int limit,
        long minimumBytes,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => AnalyzeInternal(rootPath, limit, minimumBytes, cancellationToken), cancellationToken);
    }

    private static LargeFileAnalysisResult AnalyzeInternal(
        string rootPath,
        int limit,
        long minimumBytes,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(rootPath))
        {
            return new LargeFileAnalysisResult([], 0, 0, 0);
        }

        var queue = new PriorityQueue<LargeFileRecord, long>();
        var fileCount = 0;
        long scannedBytes = 0;
        var skippedDirectoryCount = 0;
        var stack = new Stack<DirectoryInfo>();
        stack.Push(new DirectoryInfo(rootPath));

        while (stack.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = stack.Pop();

            FileSystemInfo[] entries;
            try
            {
                entries = current.GetFileSystemInfos();
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                skippedDirectoryCount++;
                continue;
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                switch (entry)
                {
                    case FileInfo file:
                        fileCount++;
                        scannedBytes += file.Length;

                        if (file.Length >= minimumBytes)
                        {
                            queue.Enqueue(
                                new LargeFileRecord(file.FullName, file.Length, file.LastWriteTimeUtc),
                                file.Length);

                            if (queue.Count > limit)
                            {
                                queue.Dequeue();
                            }
                        }

                        break;
                    case DirectoryInfo directory:
                        if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
                        {
                            stack.Push(directory);
                        }

                        break;
                }
            }
        }

        var largestFiles = queue.UnorderedItems
            .Select(item => item.Element)
            .OrderByDescending(file => file.SizeBytes)
            .ToList();

        return new LargeFileAnalysisResult(largestFiles, fileCount, scannedBytes, skippedDirectoryCount);
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or SecurityException;
    }
}
