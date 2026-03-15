using System.IO;
using System.Security;
using SonyDevCleaner.App.Models;

namespace SonyDevCleaner.App.Services;

public sealed class DirectoryCleanerTarget : CleanerTarget
{
    private readonly string[] _rootPaths;

    public DirectoryCleanerTarget(
        string id,
        string displayName,
        string description,
        IEnumerable<string> rootPaths,
        bool requiresElevation = false)
        : base(id, displayName, description, requiresElevation)
    {
        _rootPaths = rootPaths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public override Task<CleanerScanSummary> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => ScanInternal(cancellationToken), cancellationToken);
    }

    public override Task<CleanerExecutionResult> CleanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() => CleanInternal(cancellationToken), cancellationToken);
    }

    private CleanerScanSummary ScanInternal(CancellationToken cancellationToken)
    {
        long reclaimableBytes = 0;
        var itemCount = 0;
        var deniedDirectories = 0;
        var foundAnyRoot = false;

        foreach (var rootPath in _rootPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rootDirectory = new DirectoryInfo(rootPath);
            if (!rootDirectory.Exists)
            {
                continue;
            }

            foundAnyRoot = true;
            EnumerateTree(
                rootDirectory,
                file =>
                {
                    reclaimableBytes += file.Length;
                    itemCount++;
                },
                onDirectoryVisited: null,
                onError: _ => deniedDirectories++);
        }

        return new CleanerScanSummary(
            Id,
            DisplayName,
            Description,
            reclaimableBytes,
            itemCount,
            BuildScanStatus(foundAnyRoot, itemCount, deniedDirectories),
            foundAnyRoot && itemCount > 0,
            RequiresElevation);
    }

    private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
    {
        long deletedBytes = 0;
        var deletedItems = 0;
        var failedItems = 0;
        var foundAnyRoot = false;
        var rootSet = new HashSet<string>(_rootPaths.Select(NormalizePath), StringComparer.OrdinalIgnoreCase);

        foreach (var rootPath in _rootPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rootDirectory = new DirectoryInfo(rootPath);
            if (!rootDirectory.Exists)
            {
                continue;
            }

            foundAnyRoot = true;
            var directories = new List<DirectoryInfo>();

            EnumerateTree(
                rootDirectory,
                file =>
                {
                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                        {
                            file.Attributes = FileAttributes.Normal;
                        }

                        var fileLength = file.Length;
                        file.Delete();
                        deletedBytes += fileLength;
                        deletedItems++;
                    }
                    catch (Exception exception) when (IsRecoverable(exception))
                    {
                        failedItems++;
                    }
                },
                dir => directories.Add(dir),
                _ => failedItems++);

            foreach (var directory in directories.OrderByDescending(dir => dir.FullName.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (rootSet.Contains(NormalizePath(directory.FullName)))
                {
                    continue;
                }

                try
                {
                    if (!directory.Exists)
                    {
                        continue;
                    }

                    if ((directory.Attributes & FileAttributes.ReadOnly) != 0)
                    {
                        directory.Attributes &= ~FileAttributes.ReadOnly;
                    }

                    directory.Delete(false);
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    _ = exception;
                }
            }
        }

        var message = !foundAnyRoot
            ? "Folder not found."
            : failedItems > 0
                ? $"Removed {deletedItems:N0} files, skipped {failedItems:N0} locked or restricted items."
                : deletedItems == 0
                    ? "Nothing removed."
                    : $"Removed {deletedItems:N0} files.";

        return new CleanerExecutionResult(
            Id,
            DisplayName,
            deletedBytes,
            deletedItems,
            failedItems,
            message);
    }

    private static void EnumerateTree(
        DirectoryInfo rootDirectory,
        Action<FileInfo> onFile,
        Action<DirectoryInfo>? onDirectoryVisited,
        Action<Exception> onError)
    {
        var stack = new Stack<DirectoryInfo>();
        stack.Push(rootDirectory);

        while (stack.Count > 0)
        {
            var current = stack.Pop();
            onDirectoryVisited?.Invoke(current);

            FileSystemInfo[] entries;
            try
            {
                entries = current.GetFileSystemInfos();
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                onError(exception);
                continue;
            }

            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case FileInfo file:
                        onFile(file);
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
    }

    private static string NormalizePath(string path)
    {
        return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
    }

    private static string BuildScanStatus(bool foundAnyRoot, int itemCount, int deniedDirectories)
    {
        if (!foundAnyRoot)
        {
            return "Folder not found.";
        }

        if (deniedDirectories > 0 && itemCount > 0)
        {
            return $"Partial access. Skipped {deniedDirectories:N0} restricted folders.";
        }

        if (deniedDirectories > 0)
        {
            return "Access restricted.";
        }

        return itemCount == 0 ? "Nothing to clean." : "Ready to clean.";
    }

    private static bool IsRecoverable(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or SecurityException;
    }
}
