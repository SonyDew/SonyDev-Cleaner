using System.IO;
using System.Security;
using SonyDevCleaner.App.Models;

namespace SonyDevCleaner.App.Services;

public static class CleanerCatalog
{
    public static IReadOnlyList<CleanerTarget> CreateDefaultTargets()
    {
        var windowsPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        return
        [
            new DirectoryCleanerTarget(
                "user-temp",
                "User temp",
                "Temporary files created by apps in the current user profile.",
                [Path.GetTempPath()]),
            new DirectoryCleanerTarget(
                "windows-temp",
                "Windows temp",
                "Shared temporary files. Some items may require administrator access.",
                [Path.Combine(windowsPath, "Temp")],
                requiresElevation: true),
            new PatternCleanerTarget(
                "thumbnail-cache",
                "Thumbnail cache",
                "Windows thumbnail database files stored per folder.",
                [
                    PathPatternSpec.FilePattern("%LOCALAPPDATA%\\Microsoft\\Windows\\Explorer\\thumbcache_*.db")
                ]),
            new PatternCleanerTarget(
                "font-cache",
                "Font cache",
                "Windows font metadata cache, rebuilt automatically on next boot.",
                [
                    PathPatternSpec.DirectoryContents("%WINDIR%\\ServiceProfiles\\LocalService\\AppData\\Local\\FontCache\\*"),
                    PathPatternSpec.ExactFile("%WINDIR%\\System32\\FNTCACHE.DAT")
                ]),
            new PatternCleanerTarget(
                "windows-update-cache",
                "Windows Update cache",
                "Downloaded update packages that have already been installed.",
                [
                    PathPatternSpec.DirectoryContents("%WINDIR%\\SoftwareDistribution\\Download\\*")
                ],
                requiresElevation: true),
            new PatternCleanerTarget(
                "log-files",
                "Log files",
                "Application and system log files (.log, .etl) older than 7 days.",
                [
                    PathPatternSpec.FilePattern("%TEMP%\\*.log", OlderThanDays(7)),
                    PathPatternSpec.FilePattern("%TEMP%\\*.etl", OlderThanDays(7)),
                    PathPatternSpec.FilePattern("%LOCALAPPDATA%\\Temp\\*.log", OlderThanDays(7))
                ]),
            new PatternCleanerTarget(
                "prefetch-files",
                "Prefetch files",
                "Windows app launch prefetch data. Rebuilt automatically.",
                [
                    PathPatternSpec.FilePattern("%WINDIR%\\Prefetch\\*.pf")
                ],
                requiresElevation: true),
            new DirectoryCleanerTarget(
                "shader-cache",
                "DirectX shader cache",
                "GPU shader cache. Windows and games rebuild this automatically.",
                [Path.Combine(localAppData, "D3DSCache")]),
            new DirectoryCleanerTarget(
                "crash-dumps",
                "Crash dumps",
                "Application dump files stored after crashes in the current user profile.",
                [Path.Combine(localAppData, "CrashDumps")]),
            new PatternCleanerTarget(
                "windows-error-reporting",
                "Windows Error Reporting",
                "Crash report dumps collected by Windows. Safe to remove.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\Windows\\WER\\ReportArchive\\*"),
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\Windows\\WER\\ReportQueue\\*"),
                    PathPatternSpec.DirectoryContents("%PROGRAMDATA%\\Microsoft\\Windows\\WER\\ReportArchive\\*"),
                    PathPatternSpec.DirectoryContents("%PROGRAMDATA%\\Microsoft\\Windows\\WER\\ReportQueue\\*")
                ]),
            new EmptyFolderCleanerTarget(
                "empty-folders",
                "Empty folders",
                "Empty directories inside temporary folders.",
                [
                    "%TEMP%",
                    "%LOCALAPPDATA%\\Temp"
                ]),
            new PatternCleanerTarget(
                "chrome-cache",
                "Chrome cache",
                "Google Chrome cached web content.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Cache\\Cache_Data\\*"),
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Google\\Chrome\\User Data\\Default\\Code Cache\\js\\*")
                ]),
            new PatternCleanerTarget(
                "edge-cache",
                "Edge cache",
                "Microsoft Edge cached web content.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Cache\\Cache_Data\\*"),
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\Edge\\User Data\\Default\\Code Cache\\js\\*")
                ]),
            new PatternCleanerTarget(
                "firefox-cache",
                "Firefox cache",
                "Mozilla Firefox cached web content.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Mozilla\\Firefox\\Profiles\\*\\cache2\\entries\\*")
                ]),
            new PatternCleanerTarget(
                "discord-cache",
                "Discord cache",
                "Discord cached images, videos and app data.",
                [
                    PathPatternSpec.DirectoryContents("%APPDATA%\\discord\\Cache\\Cache_Data\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\discord\\Code Cache\\js\\*")
                ]),
            new PatternCleanerTarget(
                "steam-download-cache",
                "Steam download cache",
                "Steam package download cache. Games are not affected.",
                [
                    PathPatternSpec.DirectoryContents("%PROGRAMFILES(X86)%\\Steam\\depotcache\\*"),
                    PathPatternSpec.DirectoryContents("%PROGRAMFILES(X86)%\\Steam\\logs\\*")
                ]),
            new PatternCleanerTarget(
                "teams-cache",
                "Teams cache",
                "Microsoft Teams cached data and media files.",
                [
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\Cache\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\blob_storage\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\databases\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\GPUCache\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\IndexedDB\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\Local Storage\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Microsoft\\Teams\\tmp\\*")
                ]),
            new PatternCleanerTarget(
                "onedrive-cache",
                "OneDrive cache",
                "OneDrive temporary sync and metadata cache.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\OneDrive\\logs\\*"),
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\OneDrive\\setup\\logs\\*")
                ]),
            new PatternCleanerTarget(
                "spotify-cache",
                "Spotify cache",
                "Spotify streamed audio and image cache. Rebuilt on playback.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Spotify\\Storage\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Spotify\\storage\\*")
                ]),
            new VisualStudioCacheCleanerTarget(
                "visual-studio-cache",
                "Visual Studio cache",
                "VS solution cache folders (.vs) and build intermediates."),
            new PatternCleanerTarget(
                "vs-code-cache",
                "VS Code cache",
                "Visual Studio Code cached extensions and workspace data.",
                [
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Code\\Cache\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Code\\CachedData\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Code\\CachedExtensionVSIXs\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Code\\Code Cache\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\Code\\GPUCache\\*")
                ]),
            new PatternCleanerTarget(
                "npm-cache",
                "npm cache",
                "Node.js package manager download cache.",
                [
                    PathPatternSpec.DirectoryContents("%APPDATA%\\npm-cache\\_npx\\*"),
                    PathPatternSpec.DirectoryContents("%APPDATA%\\npm-cache\\*")
                ]),
            new PatternCleanerTarget(
                "pip-cache",
                "pip cache",
                "Python pip package download cache.",
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\pip\\cache\\*")
                ]),
            new NuGetCacheCleanerTarget(
                "nuget-cache",
                "NuGet cache",
                ".NET NuGet package download cache."),
            new RecycleBinCleanerTarget()
        ];
    }

    private static Func<FileInfo, bool> OlderThanDays(int days)
    {
        return file => file.LastWriteTime < DateTime.Now.AddDays(-days);
    }

    private static string CatalogBuildScanStatus(bool foundAnyRoot, int itemCount, int deniedCount)
    {
        if (!foundAnyRoot)
        {
            return "Folder not found.";
        }

        if (deniedCount > 0 && itemCount > 0)
        {
            return $"Partial access. Skipped {deniedCount:N0} restricted folders.";
        }

        if (deniedCount > 0)
        {
            return "Access restricted.";
        }

        return itemCount == 0 ? "Nothing to clean." : "Ready to clean.";
    }

    private static bool CatalogIsRecoverable(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or SecurityException;
    }

    private static string CatalogNormalizePath(string path)
    {
        return Path.GetFullPath(path);
    }

    private static bool StatusIndicatesRestricted(string status)
    {
        return status.Contains("restricted", StringComparison.OrdinalIgnoreCase)
            || status.Contains("Partial access", StringComparison.OrdinalIgnoreCase);
    }

    private static bool StatusFoundAnyRoot(string status)
    {
        return !status.Equals("Folder not found.", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CountBox
    {
        public int Value { get; set; }
    }

    private enum PathMatchMode
    {
        ExactFile,
        FilePattern,
        DirectoryContents
    }

    private sealed record PathPatternSpec(string PathPattern, PathMatchMode Mode, Func<FileInfo, bool>? Filter = null)
    {
        public static PathPatternSpec ExactFile(string pathPattern)
        {
            return new PathPatternSpec(pathPattern, PathMatchMode.ExactFile);
        }

        public static PathPatternSpec FilePattern(string pathPattern, Func<FileInfo, bool>? filter = null)
        {
            return new PathPatternSpec(pathPattern, PathMatchMode.FilePattern, filter);
        }

        public static PathPatternSpec DirectoryContents(string pathPattern, Func<FileInfo, bool>? filter = null)
        {
            return new PathPatternSpec(pathPattern, PathMatchMode.DirectoryContents, filter);
        }
    }

    private sealed class EmptyFolderCleanerTarget : CleanerTarget
    {
        private readonly string[] _rootPaths;

        public EmptyFolderCleanerTarget(string id, string displayName, string description, IEnumerable<string> rootPaths)
            : base(id, displayName, description)
        {
            _rootPaths = rootPaths
                .Select(Environment.ExpandEnvironmentVariables)
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
            var emptyDirectories = CollectEmptyDirectories(cancellationToken, out var foundAnyRoot, out var deniedCount);

            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                0,
                emptyDirectories.Count,
                CatalogBuildScanStatus(foundAnyRoot, emptyDirectories.Count, deniedCount),
                foundAnyRoot && emptyDirectories.Count > 0,
                RequiresElevation);
        }

        private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
        {
            var emptyDirectories = CollectEmptyDirectories(cancellationToken, out var foundAnyRoot, out var deniedCount);
            var deletedItems = 0;
            var failedItems = deniedCount;

            foreach (var directory in emptyDirectories.OrderByDescending(item => item.FullName.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (directory.Exists)
                    {
                        directory.Delete(false);
                        deletedItems++;
                    }
                }
                catch (Exception exception) when (CatalogIsRecoverable(exception))
                {
                    failedItems++;
                }
            }

            var message = !foundAnyRoot
                ? "Folder not found."
                : failedItems > 0
                    ? $"Removed {deletedItems:N0} folders, skipped {failedItems:N0} locked or restricted items."
                    : deletedItems == 0
                        ? "Nothing removed."
                        : $"Removed {deletedItems:N0} folders.";

            return new CleanerExecutionResult(Id, DisplayName, 0, deletedItems, failedItems, message);
        }

        private List<DirectoryInfo> CollectEmptyDirectories(
            CancellationToken cancellationToken,
            out bool foundAnyRoot,
            out int deniedCount)
        {
            foundAnyRoot = false;
            deniedCount = 0;
            var results = new List<DirectoryInfo>();

            foreach (var rootPath in _rootPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var rootDirectory = new DirectoryInfo(rootPath);
                if (!rootDirectory.Exists)
                {
                    continue;
                }

                foundAnyRoot = true;

                DirectoryInfo[] children;
                try
                {
                    children = rootDirectory.GetDirectories();
                }
                catch (Exception exception) when (CatalogIsRecoverable(exception))
                {
                    deniedCount++;
                    continue;
                }

                foreach (var child in children)
                {
                    EvaluateDirectory(child, results, ref deniedCount, cancellationToken);
                }
            }

            return results;
        }

        private static bool EvaluateDirectory(
            DirectoryInfo directory,
            List<DirectoryInfo> emptyDirectories,
            ref int deniedCount,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                {
                    return true;
                }
            }
            catch (Exception exception) when (CatalogIsRecoverable(exception))
            {
                deniedCount++;
                return true;
            }

            FileSystemInfo[] entries;
            try
            {
                entries = directory.GetFileSystemInfos();
            }
            catch (Exception exception) when (CatalogIsRecoverable(exception))
            {
                deniedCount++;
                return true;
            }

            var containsAnyFiles = false;
            foreach (var entry in entries)
            {
                switch (entry)
                {
                    case FileInfo:
                        containsAnyFiles = true;
                        break;
                    case DirectoryInfo childDirectory:
                        if (EvaluateDirectory(childDirectory, emptyDirectories, ref deniedCount, cancellationToken))
                        {
                            containsAnyFiles = true;
                        }

                        break;
                }
            }

            if (!containsAnyFiles)
            {
                emptyDirectories.Add(directory);
            }

            return containsAnyFiles;
        }
    }

    private sealed class VisualStudioCacheCleanerTarget : CleanerTarget
    {
        private readonly string[] _searchRoots;
        private readonly PatternCleanerTarget _componentCacheTarget;

        public VisualStudioCacheCleanerTarget(string id, string displayName, string description)
            : base(id, displayName, description)
        {
            _searchRoots =
            [
                Environment.ExpandEnvironmentVariables("%USERPROFILE%\\source"),
                Environment.ExpandEnvironmentVariables("%USERPROFILE%\\Documents")
            ];

            _componentCacheTarget = new PatternCleanerTarget(
                $"{id}-component-model",
                displayName,
                description,
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\Microsoft\\VisualStudio\\*\\ComponentModelCache\\*")
                ]);
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
            var roots = FindVsRoots(cancellationToken, out var foundAnyVsRoot, out var deniedCount);
            long reclaimableBytes = 0;
            var itemCount = 0;
            var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var deniedCounter = new CountBox { Value = deniedCount };

            foreach (var root in roots)
            {
                foreach (var file in EnumerateFilesUnder(root, cancellationToken, deniedCounter))
                {
                    var normalizedPath = CatalogNormalizePath(file.FullName);
                    if (!seenFiles.Add(normalizedPath))
                    {
                        continue;
                    }

                    try
                    {
                        reclaimableBytes += file.Length;
                        itemCount++;
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        deniedCounter.Value++;
                    }
                }
            }

            var componentSummary = _componentCacheTarget.ScanAsync(cancellationToken).GetAwaiter().GetResult();
            reclaimableBytes += componentSummary.ReclaimableBytes;
            itemCount += componentSummary.ItemCount;

            var foundAnyRoot = foundAnyVsRoot || StatusFoundAnyRoot(componentSummary.Status);
            if (StatusIndicatesRestricted(componentSummary.Status))
            {
                deniedCounter.Value++;
            }

            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                reclaimableBytes,
                itemCount,
                CatalogBuildScanStatus(foundAnyRoot, itemCount, deniedCounter.Value),
                foundAnyRoot && itemCount > 0,
                RequiresElevation);
        }

        private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
        {
            var roots = FindVsRoots(cancellationToken, out var foundAnyVsRoot, out var deniedCount);
            long deletedBytes = 0;
            var deletedItems = 0;
            var failedCounter = new CountBox { Value = deniedCount };
            var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var root in roots)
            {
                var directories = new List<DirectoryInfo>();
                foreach (var file in EnumerateFilesUnder(root, cancellationToken, failedCounter, directories))
                {
                    var normalizedPath = CatalogNormalizePath(file.FullName);
                    if (!seenFiles.Add(normalizedPath))
                    {
                        continue;
                    }

                    try
                    {
                        if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                        {
                            file.Attributes = FileAttributes.Normal;
                        }

                        var fileLength = file.Length;
                        file.Delete();
                        deletedBytes += fileLength;
                        deletedItems++;
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        failedCounter.Value++;
                    }
                }

                foreach (var directory in directories
                             .Where(directory => !string.Equals(directory.FullName, root.FullName, StringComparison.OrdinalIgnoreCase))
                             .OrderByDescending(directory => directory.FullName.Length))
                {
                    try
                    {
                        if (directory.Exists)
                        {
                            directory.Delete(false);
                        }
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        _ = exception;
                    }
                }
            }

            var componentResult = _componentCacheTarget.CleanAsync(cancellationToken).GetAwaiter().GetResult();
            deletedBytes += componentResult.DeletedBytes;
            deletedItems += componentResult.DeletedItems;
            failedCounter.Value += componentResult.FailedItems;

            var foundAnyRoot = foundAnyVsRoot || componentResult.Message != "Folder not found.";
            var message = !foundAnyRoot
                ? "Folder not found."
                : failedCounter.Value > 0
                    ? $"Removed {deletedItems:N0} files, skipped {failedCounter.Value:N0} locked or restricted items."
                    : deletedItems == 0
                        ? "Nothing removed."
                        : $"Removed {deletedItems:N0} files.";

            return new CleanerExecutionResult(Id, DisplayName, deletedBytes, deletedItems, failedCounter.Value, message);
        }

        private List<DirectoryInfo> FindVsRoots(
            CancellationToken cancellationToken,
            out bool foundAnyRoot,
            out int deniedCount)
        {
            foundAnyRoot = false;
            deniedCount = 0;
            var results = new List<DirectoryInfo>();

            foreach (var searchRoot in _searchRoots)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var pathState = GetPathState(searchRoot);
                switch (pathState)
                {
                    case ExistingPathState.Restricted:
                        foundAnyRoot = true;
                        deniedCount++;
                        continue;
                    case ExistingPathState.DirectoryAccessible:
                        foundAnyRoot = true;
                        break;
                    default:
                        continue;
                }

                var stack = new Stack<DirectoryInfo>();
                stack.Push(new DirectoryInfo(searchRoot));

                while (stack.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var current = stack.Pop();
                    FileSystemInfo[] entries;
                    try
                    {
                        entries = current.GetFileSystemInfos();
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        deniedCount++;
                        continue;
                    }

                    foreach (var entry in entries)
                    {
                        if (entry is not DirectoryInfo directory)
                        {
                            continue;
                        }

                        try
                        {
                            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                            {
                                continue;
                            }
                        }
                        catch (Exception exception) when (CatalogIsRecoverable(exception))
                        {
                            deniedCount++;
                            continue;
                        }

                        if (string.Equals(directory.Name, ".vs", StringComparison.OrdinalIgnoreCase))
                        {
                            results.Add(directory);
                            continue;
                        }

                        stack.Push(directory);
                    }
                }
            }

            return results;
        }

        private static IEnumerable<FileInfo> EnumerateFilesUnder(
            DirectoryInfo root,
            CancellationToken cancellationToken,
            CountBox deniedCount,
            List<DirectoryInfo>? directories = null)
        {
            var stack = new Stack<DirectoryInfo>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = stack.Pop();
                directories?.Add(current);

                FileSystemInfo[] entries;
                try
                {
                    entries = current.GetFileSystemInfos();
                }
                catch (Exception exception) when (CatalogIsRecoverable(exception))
                {
                    deniedCount.Value++;
                    continue;
                }

                foreach (var entry in entries)
                {
                    switch (entry)
                    {
                        case FileInfo file:
                            yield return file;
                            break;
                        case DirectoryInfo directory:
                            try
                            {
                                if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
                                {
                                    stack.Push(directory);
                                }
                            }
                            catch (Exception exception) when (CatalogIsRecoverable(exception))
                            {
                                deniedCount.Value++;
                            }

                            break;
                    }
                }
            }
        }

        private static ExistingPathState GetPathState(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Directory) != 0
                    ? ExistingPathState.DirectoryAccessible
                    : ExistingPathState.FileAccessible;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
            {
                return ExistingPathState.Restricted;
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or IOException)
            {
                return ExistingPathState.Missing;
            }
        }

        private enum ExistingPathState
        {
            Missing,
            DirectoryAccessible,
            FileAccessible,
            Restricted
        }
    }

    private sealed class NuGetCacheCleanerTarget : CleanerTarget
    {
        private const long ManualReviewThresholdBytes = 10L * 1024 * 1024 * 1024;

        private readonly string _packagesRoot;
        private readonly PatternCleanerTarget _otherCachesTarget;

        public NuGetCacheCleanerTarget(string id, string displayName, string description)
            : base(id, displayName, description)
        {
            _packagesRoot = Environment.ExpandEnvironmentVariables("%USERPROFILE%\\.nuget\\packages");
            _otherCachesTarget = new PatternCleanerTarget(
                $"{id}-other",
                displayName,
                description,
                [
                    PathPatternSpec.DirectoryContents("%LOCALAPPDATA%\\NuGet\\v3-cache\\*"),
                    PathPatternSpec.DirectoryContents("%TEMP%\\NuGetScratch\\*")
                ]);
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
            var packageSummary = ScanPackagesRoot(cancellationToken);
            var otherSummary = _otherCachesTarget.ScanAsync(cancellationToken).GetAwaiter().GetResult();

            var totalBytes = packageSummary.ReclaimableBytes + otherSummary.ReclaimableBytes;
            var totalItems = packageSummary.ItemCount + otherSummary.ItemCount;
            var foundAnyRoot = StatusFoundAnyRoot(packageSummary.Status) || StatusFoundAnyRoot(otherSummary.Status);
            var deniedCount = 0;

            if (StatusIndicatesRestricted(packageSummary.Status))
            {
                deniedCount++;
            }

            if (StatusIndicatesRestricted(otherSummary.Status))
            {
                deniedCount++;
            }

            if (packageSummary.ReclaimableBytes > ManualReviewThresholdBytes)
            {
                return new CleanerScanSummary(
                    Id,
                    DisplayName,
                    Description,
                    totalBytes,
                    totalItems,
                    "Large package cache — review manually",
                    false,
                    RequiresElevation);
            }

            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                totalBytes,
                totalItems,
                CatalogBuildScanStatus(foundAnyRoot, totalItems, deniedCount),
                foundAnyRoot && totalItems > 0,
                RequiresElevation);
        }

        private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
        {
            var packageSummary = ScanPackagesRoot(cancellationToken);
            if (packageSummary.ReclaimableBytes > ManualReviewThresholdBytes)
            {
                return new CleanerExecutionResult(Id, DisplayName, 0, 0, 0, "Large package cache — review manually.");
            }

            long deletedBytes = 0;
            var deletedItems = 0;
            var failedCounter = new CountBox();
            var foundAnyRoot = false;

            var packageDirectory = new DirectoryInfo(_packagesRoot);
            if (packageDirectory.Exists)
            {
                foundAnyRoot = true;
                var directories = new List<DirectoryInfo>();

                foreach (var file in EnumerateFilesUnder(packageDirectory, cancellationToken, failedCounter, directories))
                {
                    try
                    {
                        if ((file.Attributes & FileAttributes.ReadOnly) != 0)
                        {
                            file.Attributes = FileAttributes.Normal;
                        }

                        var fileLength = file.Length;
                        file.Delete();
                        deletedBytes += fileLength;
                        deletedItems++;
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        failedCounter.Value++;
                    }
                }

                foreach (var directory in directories
                             .Where(directory => !string.Equals(directory.FullName, packageDirectory.FullName, StringComparison.OrdinalIgnoreCase))
                             .OrderByDescending(directory => directory.FullName.Length))
                {
                    try
                    {
                        if (directory.Exists)
                        {
                            directory.Delete(false);
                        }
                    }
                    catch (Exception exception) when (CatalogIsRecoverable(exception))
                    {
                        _ = exception;
                    }
                }
            }

            var otherResult = _otherCachesTarget.CleanAsync(cancellationToken).GetAwaiter().GetResult();
            deletedBytes += otherResult.DeletedBytes;
            deletedItems += otherResult.DeletedItems;
            failedCounter.Value += otherResult.FailedItems;
            foundAnyRoot |= otherResult.Message != "Folder not found.";

            var message = !foundAnyRoot
                ? "Folder not found."
                : failedCounter.Value > 0
                    ? $"Removed {deletedItems:N0} files, skipped {failedCounter.Value:N0} locked or restricted items."
                    : deletedItems == 0
                        ? "Nothing removed."
                        : $"Removed {deletedItems:N0} files.";

            return new CleanerExecutionResult(Id, DisplayName, deletedBytes, deletedItems, failedCounter.Value, message);
        }

        private CleanerScanSummary ScanPackagesRoot(CancellationToken cancellationToken)
        {
            var packageDirectory = new DirectoryInfo(_packagesRoot);
            if (!packageDirectory.Exists)
            {
                return new CleanerScanSummary(Id, DisplayName, Description, 0, 0, "Folder not found.", false, RequiresElevation);
            }

            long reclaimableBytes = 0;
            var itemCount = 0;
            var deniedCounter = new CountBox();

            foreach (var file in EnumerateFilesUnder(packageDirectory, cancellationToken, deniedCounter))
            {
                try
                {
                    reclaimableBytes += file.Length;
                    itemCount++;
                }
                catch (Exception exception) when (CatalogIsRecoverable(exception))
                {
                    deniedCounter.Value++;
                }
            }

            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                reclaimableBytes,
                itemCount,
                CatalogBuildScanStatus(true, itemCount, deniedCounter.Value),
                itemCount > 0,
                RequiresElevation);
        }

        private static IEnumerable<FileInfo> EnumerateFilesUnder(
            DirectoryInfo root,
            CancellationToken cancellationToken,
            CountBox deniedCount,
            List<DirectoryInfo>? directories = null)
        {
            var stack = new Stack<DirectoryInfo>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var current = stack.Pop();
                directories?.Add(current);

                FileSystemInfo[] entries;
                try
                {
                    entries = current.GetFileSystemInfos();
                }
                catch (Exception exception) when (CatalogIsRecoverable(exception))
                {
                    deniedCount.Value++;
                    continue;
                }

                foreach (var entry in entries)
                {
                    switch (entry)
                    {
                        case FileInfo file:
                            yield return file;
                            break;
                        case DirectoryInfo directory:
                            try
                            {
                                if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
                                {
                                    stack.Push(directory);
                                }
                            }
                            catch (Exception exception) when (CatalogIsRecoverable(exception))
                            {
                                deniedCount.Value++;
                            }

                            break;
                    }
                }
            }
        }
    }

    private sealed class PatternCleanerTarget : CleanerTarget
    {
        private readonly PathPatternSpec[] _specs;

        public PatternCleanerTarget(
            string id,
            string displayName,
            string description,
            IEnumerable<PathPatternSpec> specs,
            bool requiresElevation = false)
            : base(id, displayName, description, requiresElevation)
        {
            _specs = specs.ToArray();
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
            var state = new EnumerationState();
            var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in _specs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var file in EnumerateMatches(spec, state, cancellationToken))
                {
                    var normalizedPath = NormalizePath(file.FullName);
                    if (!seenFiles.Add(normalizedPath))
                    {
                        continue;
                    }

                    try
                    {
                        reclaimableBytes += file.Length;
                        itemCount++;
                    }
                    catch (Exception exception) when (IsRecoverable(exception))
                    {
                        state.DeniedCount++;
                    }
                }
            }

            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                reclaimableBytes,
                itemCount,
                BuildScanStatus(state.FoundAnyRoot, itemCount, state.DeniedCount),
                state.FoundAnyRoot && itemCount > 0,
                RequiresElevation);
        }

        private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
        {
            long deletedBytes = 0;
            var deletedItems = 0;
            var failedItems = 0;
            var state = new EnumerationState();
            var seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var spec in _specs)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var file in EnumerateMatches(spec, state, cancellationToken))
                {
                    var normalizedPath = NormalizePath(file.FullName);
                    if (!seenFiles.Add(normalizedPath))
                    {
                        continue;
                    }

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
                }
            }

            failedItems += state.DeniedCount;

            var message = !state.FoundAnyRoot
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

        private static IEnumerable<FileInfo> EnumerateMatches(
            PathPatternSpec spec,
            EnumerationState state,
            CancellationToken cancellationToken)
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(spec.PathPattern);
            var fullPath = Path.GetFullPath(expandedPath);
            var pathRoot = Path.GetPathRoot(fullPath);

            if (string.IsNullOrWhiteSpace(pathRoot))
            {
                yield break;
            }

            var relativePath = fullPath[pathRoot.Length..];
            var segments = relativePath
                .Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                yield break;
            }

            foreach (var file in EnumerateMatches(pathRoot, segments, 0, spec, state, cancellationToken))
            {
                yield return file;
            }
        }

        private static IEnumerable<FileInfo> EnumerateMatches(
            string currentPath,
            string[] segments,
            int index,
            PathPatternSpec spec,
            EnumerationState state,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (index >= segments.Length)
            {
                yield break;
            }

            var segment = segments[index];
            var isLastSegment = index == segments.Length - 1;

            if (isLastSegment)
            {
                switch (spec.Mode)
                {
                    case PathMatchMode.ExactFile:
                        foreach (var file in EnumerateExactFile(Path.Combine(currentPath, segment), spec.Filter, state))
                        {
                            yield return file;
                        }

                        yield break;
                    case PathMatchMode.FilePattern:
                        foreach (var file in EnumerateFilePattern(currentPath, segment, spec.Filter, state))
                        {
                            yield return file;
                        }

                        yield break;
                    case PathMatchMode.DirectoryContents:
                        foreach (var file in EnumerateDirectoryContents(currentPath, spec.Filter, state, cancellationToken))
                        {
                            yield return file;
                        }

                        yield break;
                }
            }

            if (!ContainsWildcard(segment))
            {
                foreach (var file in EnumerateMatches(Path.Combine(currentPath, segment), segments, index + 1, spec, state, cancellationToken))
                {
                    yield return file;
                }

                yield break;
            }

            var currentState = GetPathState(currentPath);
            if (currentState is ExistingPathState.Restricted)
            {
                state.FoundAnyRoot = true;
                state.DeniedCount++;
                yield break;
            }

            if (currentState is not ExistingPathState.DirectoryAccessible)
            {
                yield break;
            }

            state.FoundAnyRoot = true;

            IEnumerable<string> directories;
            try
            {
                directories = Directory.EnumerateDirectories(currentPath, segment, SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                state.DeniedCount++;
                yield break;
            }

            foreach (var directory in directories)
            {
                DirectoryInfo directoryInfo;
                try
                {
                    directoryInfo = new DirectoryInfo(directory);
                    if ((directoryInfo.Attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }
                }
                catch (Exception exception) when (IsRecoverable(exception))
                {
                    state.DeniedCount++;
                    continue;
                }

                foreach (var file in EnumerateMatches(directoryInfo.FullName, segments, index + 1, spec, state, cancellationToken))
                {
                    yield return file;
                }
            }
        }

        private static IEnumerable<FileInfo> EnumerateExactFile(
            string filePath,
            Func<FileInfo, bool>? filter,
            EnumerationState state)
        {
            var pathState = GetPathState(filePath);
            switch (pathState)
            {
                case ExistingPathState.Restricted:
                    state.FoundAnyRoot = true;
                    state.DeniedCount++;
                    yield break;
                case ExistingPathState.FileAccessible:
                    state.FoundAnyRoot = true;
                    break;
                default:
                    yield break;
            }

            var file = new FileInfo(filePath);
            if (ShouldInclude(file, filter, state))
            {
                yield return file;
            }
        }

        private static IEnumerable<FileInfo> EnumerateFilePattern(
            string directoryPath,
            string searchPattern,
            Func<FileInfo, bool>? filter,
            EnumerationState state)
        {
            var pathState = GetPathState(directoryPath);
            switch (pathState)
            {
                case ExistingPathState.Restricted:
                    state.FoundAnyRoot = true;
                    state.DeniedCount++;
                    yield break;
                case ExistingPathState.DirectoryAccessible:
                    state.FoundAnyRoot = true;
                    break;
                default:
                    yield break;
            }

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directoryPath, searchPattern, SearchOption.TopDirectoryOnly);
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                state.DeniedCount++;
                yield break;
            }

            foreach (var filePath in files)
            {
                var file = new FileInfo(filePath);
                if (ShouldInclude(file, filter, state))
                {
                    yield return file;
                }
            }
        }

        private static IEnumerable<FileInfo> EnumerateDirectoryContents(
            string rootDirectoryPath,
            Func<FileInfo, bool>? filter,
            EnumerationState state,
            CancellationToken cancellationToken)
        {
            var pathState = GetPathState(rootDirectoryPath);
            switch (pathState)
            {
                case ExistingPathState.Restricted:
                    state.FoundAnyRoot = true;
                    state.DeniedCount++;
                    yield break;
                case ExistingPathState.DirectoryAccessible:
                    state.FoundAnyRoot = true;
                    break;
                default:
                    yield break;
            }

            var stack = new Stack<DirectoryInfo>();
            stack.Push(new DirectoryInfo(rootDirectoryPath));

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
                    state.DeniedCount++;
                    continue;
                }

                foreach (var entry in entries)
                {
                    switch (entry)
                    {
                        case FileInfo file when ShouldInclude(file, filter, state):
                            yield return file;
                            break;
                        case DirectoryInfo directory:
                            try
                            {
                                if ((directory.Attributes & FileAttributes.ReparsePoint) == 0)
                                {
                                    stack.Push(directory);
                                }
                            }
                            catch (Exception exception) when (IsRecoverable(exception))
                            {
                                state.DeniedCount++;
                            }

                            break;
                    }
                }
            }
        }

        private static bool ShouldInclude(FileInfo file, Func<FileInfo, bool>? filter, EnumerationState state)
        {
            try
            {
                return filter?.Invoke(file) ?? true;
            }
            catch (Exception exception) when (IsRecoverable(exception))
            {
                state.DeniedCount++;
                return false;
            }
        }

        private static ExistingPathState GetPathState(string path)
        {
            try
            {
                var attributes = File.GetAttributes(path);
                return (attributes & FileAttributes.Directory) != 0
                    ? ExistingPathState.DirectoryAccessible
                    : ExistingPathState.FileAccessible;
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException or SecurityException)
            {
                return ExistingPathState.Restricted;
            }
            catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or IOException)
            {
                return ExistingPathState.Missing;
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path);
        }

        private static bool ContainsWildcard(string segment)
        {
            return segment.Contains('*') || segment.Contains('?');
        }

        private static string BuildScanStatus(bool foundAnyRoot, int itemCount, int deniedCount)
        {
            if (!foundAnyRoot)
            {
                return "Folder not found.";
            }

            if (deniedCount > 0 && itemCount > 0)
            {
                return $"Partial access. Skipped {deniedCount:N0} restricted folders.";
            }

            if (deniedCount > 0)
            {
                return "Access restricted.";
            }

            return itemCount == 0 ? "Nothing to clean." : "Ready to clean.";
        }

        private static bool IsRecoverable(Exception exception)
        {
            return exception is IOException or UnauthorizedAccessException or SecurityException;
        }

        private sealed class EnumerationState
        {
            public bool FoundAnyRoot { get; set; }

            public int DeniedCount { get; set; }
        }

        private enum ExistingPathState
        {
            Missing,
            DirectoryAccessible,
            FileAccessible,
            Restricted
        }
    }
}
