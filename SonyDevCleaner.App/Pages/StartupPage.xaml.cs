using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;

namespace SonyDevCleaner.App.Pages;

public partial class StartupPage : UserControl
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedRunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";

    private readonly ObservableCollection<StartupEntry> _entries = [];
    private readonly bool _isElevated;

    public StartupPage()
    {
        InitializeComponent();
        _isElevated = IsElevated();
        StartupGrid.ItemsSource = _entries;
        Loaded += StartupPage_Loaded;
    }

    private async void StartupPage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadStartupItemsAsync();
    }

    private async Task LoadStartupItemsAsync()
    {
        LoadingText.Visibility = Visibility.Visible;
        StartupGrid.IsEnabled = false;

        try
        {
            var items = await Task.Run(LoadStartupItems);

            _entries.Clear();
            foreach (var item in items
                         .OrderByDescending(entry => entry.Enabled)
                         .ThenBy(entry => entry.Type)
                         .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase))
            {
                _entries.Add(item);
            }
        }
        finally
        {
            StartupGrid.IsEnabled = true;
            LoadingText.Visibility = Visibility.Collapsed;
        }
    }

    private List<StartupEntry> LoadStartupItems()
    {
        var items = new List<StartupEntry>();

        items.AddRange(LoadRegistryEntries(RegistryHive.CurrentUser, "Registry (User)"));
        items.AddRange(LoadRegistryEntries(RegistryHive.LocalMachine, "Registry (System)"));
        items.AddRange(LoadStartupFolderEntries(
            Environment.ExpandEnvironmentVariables("%APPDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup"),
            requiresElevation: false));
        items.AddRange(LoadStartupFolderEntries(
            Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\Microsoft\\Windows\\Start Menu\\Programs\\Startup"),
            requiresElevation: !_isElevated));

        return items;
    }

    private IEnumerable<StartupEntry> LoadRegistryEntries(RegistryHive hive, string typeLabel)
    {
        Microsoft.Win32.RegistryKey? runKey = null;

        try
        {
            runKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default).OpenSubKey(RunKeyPath, false);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        using (runKey)
        {
            if (runKey is null)
            {
                yield break;
            }

            foreach (var valueName in runKey.GetValueNames())
            {
                var value = runKey.GetValue(valueName)?.ToString() ?? string.Empty;
                yield return new StartupEntry
                {
                    Enabled = ReadApprovedState(hive, valueName),
                    Name = valueName,
                    Publisher = GetPublisherFromCommand(value),
                    Location = value,
                    Type = typeLabel,
                    RegistryKey = RunKeyPath,
                    RegistryName = valueName,
                    ApprovedRegistryKey = ApprovedRunKeyPath,
                    RequiresElevation = hive == RegistryHive.LocalMachine && !_isElevated
                };
            }
        }
    }

    private IEnumerable<StartupEntry> LoadStartupFolderEntries(string folderPath, bool requiresElevation)
    {
        if (!Directory.Exists(folderPath))
        {
            yield break;
        }

        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(folderPath, "*", SearchOption.TopDirectoryOnly);
        }
        catch (UnauthorizedAccessException)
        {
            yield break;
        }

        foreach (var filePath in files)
        {
            if (!IsStartupFolderItem(filePath))
            {
                continue;
            }

            var enabled = !filePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
            var publisherPath = enabled ? filePath : filePath[..^".disabled".Length];

            yield return new StartupEntry
            {
                Enabled = enabled,
                Name = GetStartupDisplayName(publisherPath),
                Publisher = GetPublisherFromPath(publisherPath),
                Location = filePath,
                Type = "Startup Folder",
                FilePath = filePath,
                RequiresElevation = requiresElevation
            };
        }
    }

    private bool ReadApprovedState(RegistryHive hive, string valueName)
    {
        try
        {
            using var approvedKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default)
                .OpenSubKey(ApprovedRunKeyPath, false);
            var value = approvedKey?.GetValue(valueName) as byte[];
            return value is not { Length: > 0 } || value[0] != 0x03;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private void DisableStartup_Click(object sender, RoutedEventArgs e)
    {
        ToggleSelectedEntries(false);
    }

    private void EnableStartup_Click(object sender, RoutedEventArgs e)
    {
        ToggleSelectedEntries(true);
    }

    private void ToggleSelectedEntries(bool enable)
    {
        var selected = StartupGrid.SelectedItems.Cast<StartupEntry>().ToList();
        if (selected.Count == 0)
        {
            MessageBox.Show("Select one or more startup items first.", "Startup Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (selected.Any(entry => entry.RequiresElevation))
        {
            MessageBox.Show("Requires elevation", "Startup Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            foreach (var entry in selected)
            {
                if (entry.IsFolderEntry)
                {
                    ToggleStartupFolderEntry(entry, enable);
                }
                else
                {
                    ToggleRegistryEntry(entry, enable);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show("Requires elevation", "Startup Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        catch (IOException exception)
        {
            MessageBox.Show(exception.Message, "Startup Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _ = LoadStartupItemsAsync();
    }

    private static void ToggleRegistryEntry(StartupEntry entry, bool enable)
    {
        var hive = entry.Type.Contains("(System)", StringComparison.OrdinalIgnoreCase)
            ? RegistryHive.LocalMachine
            : RegistryHive.CurrentUser;

        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
        using var approvedKey = baseKey.CreateSubKey(entry.ApprovedRegistryKey, true);
        approvedKey?.SetValue(
            entry.RegistryName,
            new byte[] { enable ? (byte)0x02 : (byte)0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 },
            RegistryValueKind.Binary);
    }

    private static void ToggleStartupFolderEntry(StartupEntry entry, bool enable)
    {
        var currentPath = entry.FilePath;
        var targetPath = enable
            ? RemoveDisabledSuffix(currentPath)
            : currentPath + ".disabled";

        if (string.Equals(currentPath, targetPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!File.Exists(currentPath))
        {
            throw new IOException("The startup item no longer exists.");
        }

        if (File.Exists(targetPath))
        {
            throw new IOException("A startup item with the target name already exists.");
        }

        File.Move(currentPath, targetPath);
    }

    private static bool IsStartupFolderItem(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        if (extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return filePath.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetStartupDisplayName(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        if (fileName.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase))
        {
            fileName = fileName[..^".disabled".Length];
        }

        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static string RemoveDisabledSuffix(string path)
    {
        return path.EndsWith(".disabled", StringComparison.OrdinalIgnoreCase)
            ? path[..^".disabled".Length]
            : path;
    }

    private static string GetPublisherFromCommand(string command)
    {
        var executablePath = TryExtractExecutablePath(command);
        return executablePath is null ? string.Empty : GetPublisherFromPath(executablePath);
    }

    private static string GetPublisherFromPath(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return string.Empty;
            }

            return FileVersionInfo.GetVersionInfo(path).CompanyName ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string? TryExtractExecutablePath(string command)
    {
        var trimmed = command.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        if (trimmed.StartsWith('"'))
        {
            var endQuote = trimmed.IndexOf('"', 1);
            if (endQuote > 1)
            {
                return trimmed[1..endQuote];
            }
        }

        foreach (var extension in new[] { ".exe", ".com", ".bat", ".cmd", ".lnk" })
        {
            var index = trimmed.IndexOf(extension, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                return trimmed[..(index + extension.Length)];
            }
        }

        return null;
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private sealed class StartupEntry
    {
        public bool Enabled { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Publisher { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string RegistryKey { get; set; } = string.Empty;

        public string RegistryName { get; set; } = string.Empty;

        public string ApprovedRegistryKey { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public bool RequiresElevation { get; set; }

        public bool IsFolderEntry => !string.IsNullOrWhiteSpace(FilePath);
    }
}
