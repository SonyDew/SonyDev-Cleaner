using Microsoft.Win32;
using SonyDevCleaner.App.Models;
using SonyDevCleaner.App.Pages;
using SonyDevCleaner.App.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;

namespace SonyDevCleaner.App;

public partial class MainWindow : Window
{
    private enum AppPage
    {
        Home,
        Cleanup,
        LargeFiles,
        Activity,
        Startup,
        DiskUsage
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    [DllImport("user32.dll")]
    private static extern bool SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttribData data);

    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaUseImmersiveDarkMode = 20;
    private const int WcaAccentPolicy = 19;

    [StructLayout(LayoutKind.Sequential)]
    private struct AccentPolicy
    {
        public int AccentState;
        public int AccentFlags;
        public int GradientColor;
        public int AnimationId;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WindowCompositionAttribData
    {
        public int Attribute;
        public IntPtr Data;
        public int SizeOfData;
    }

    private readonly IReadOnlyList<CleanerTarget> _targets;
    private readonly LargeFileAnalyzer _largeFileAnalyzer = new();
    private readonly bool _isElevated;
    private readonly List<string> _logEntries = [];
    private readonly Dictionary<AppPage, UserControl> _pages = [];
    private readonly Dictionary<AppPage, Button> _navigationButtons = [];
    private readonly ObservableCollection<CleanupRowViewModel> _cleanupRows = [];
    private readonly ObservableCollection<LargeFileRowViewModel> _largeFileRows = [];

    private readonly HomePage _homePage;
    private readonly CleanupPage _cleanupPage;
    private readonly LargeFilesPage _largeFilesPage;
    private readonly ActivityPage _activityPage;
    private readonly StartupPage _startupPage;
    private readonly DiskUsagePage _diskUsagePage;

    private bool _isUpdatingSelection;
    private bool _isBusy;
    private bool _allowClose;
    private AppPage _currentPage;

    public MainWindow()
    {
        InitializeComponent();

        _isElevated = IsProcessElevated();
        _targets = CleanerCatalog.CreateDefaultTargets();

        _homePage = new HomePage();
        _cleanupPage = new CleanupPage();
        _largeFilesPage = new LargeFilesPage();
        _activityPage = new ActivityPage();
        _startupPage = new StartupPage();
        _diskUsagePage = new DiskUsagePage();

        InitializePages();
        HookEvents();

        _largeFilesPage.FolderPath.Text = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _largeFilesPage.MinimumSize.Text = "64";

        UpdateStaticState();
        NavigateTo(AppPage.Home);
        _homePage.RefreshStats();
        UpdateSelectionSummary();
        AppendLog("SonyDev Cleaner initialized.");

        Loaded += MainWindow_Loaded;
    }

    private void InitializePages()
    {
        _pages[AppPage.Home] = _homePage;
        _pages[AppPage.Cleanup] = _cleanupPage;
        _pages[AppPage.LargeFiles] = _largeFilesPage;
        _pages[AppPage.Activity] = _activityPage;
        _pages[AppPage.Startup] = _startupPage;
        _pages[AppPage.DiskUsage] = _diskUsagePage;

        _navigationButtons[AppPage.Home] = HomeNavButton;
        _navigationButtons[AppPage.Cleanup] = CleanupNavButton;
        _navigationButtons[AppPage.LargeFiles] = LargeFilesNavButton;
        _navigationButtons[AppPage.Activity] = ActivityNavButton;
        _navigationButtons[AppPage.Startup] = StartupNavButton;
        _navigationButtons[AppPage.DiskUsage] = DiskUsageNavButton;

        _cleanupPage.Grid.ItemsSource = _cleanupRows;
        _largeFilesPage.Results.ItemsSource = _largeFileRows;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        var os = Environment.OSVersion.Version;

        var appliedModern = false;

        if (os.Build >= 22000)
        {
            try
            {
                var dark = 1;
                DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref dark, sizeof(int));

                var acrylic = 3;
                var result = DwmSetWindowAttribute(hwnd, DwmwaSystemBackdropType, ref acrylic, sizeof(int));
                appliedModern = result == 0;
            }
            catch
            {
            }
        }

        if (!appliedModern && os.Major >= 10)
        {
            try
            {
                var accent = new AccentPolicy
                {
                    AccentState = 3,
                    AccentFlags = 2,
                    GradientColor = unchecked((int)0x990D0D1A)
                };

                var accentSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttribData
                {
                    Attribute = WcaAccentPolicy,
                    Data = accentPtr,
                    SizeOfData = accentSize
                };

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(accentPtr);
            }
            catch
            {
            }
        }

        await RunScanAsync();
    }

    private void HookEvents()
    {
        _homePage.ScanButton.Click += async (_, _) => await RunScanAsync();
        _homePage.CleanupButton.Click += (_, _) => NavigateTo(AppPage.Cleanup);

        _cleanupPage.ScanActionButton.Click += async (_, _) => await RunScanAsync();
        _cleanupPage.CleanActionButton.Click += async (_, _) => await RunCleanAsync();
        _cleanupPage.SelectAll.Checked += SelectAllCheckBox_OnChanged;
        _cleanupPage.SelectAll.Unchecked += SelectAllCheckBox_OnChanged;

        _largeFilesPage.BrowseActionButton.Click += BrowseButton_OnClick;
        _largeFilesPage.AnalyzeActionButton.Click += async (_, _) => await RunLargeFileAnalysisAsync();
        _largeFilesPage.Results.MouseDoubleClick += LargeFilesResults_OnMouseDoubleClick;
    }

    private void UpdateNavigationStyles()
    {
        foreach (var pair in _navigationButtons)
        {
            pair.Value.Style = (Style)FindResource(pair.Key == _currentPage ? "NavButtonActive" : "NavButton");
        }
    }

    private void UpdateStaticState()
    {
        var modeText = _isElevated ? "Administrator" : "Standard";
        _homePage.ModeValue.Text = modeText;
        SidebarModeText.Text = $"{modeText} mode";
        SidebarSubtitleText.Text = $"Safe cleanup dashboard with glass navigation and sectioned pages.{AppPaths.PortableHint}";
    }

    private void NavigateTo(AppPage page)
    {
        _currentPage = page;
        PageContentHost.Content = _pages[page];
        UpdateNavigationStyles();
    }

    private async Task RunScanAsync()
    {
        if (_isBusy)
        {
            return;
        }

        var ownsCleanupOverlay = !_cleanupPage.IsBusyOverlayVisible;
        if (ownsCleanupOverlay)
        {
            _cleanupPage.ShowBusyOverlay("Scanning...");
        }

        SetBusyState(true, "Scanning cleanup categories...");
        AppendLog("Starting cleanup scan.");

        try
        {
            var results = new List<CleanerRowState>(_targets.Count);
            foreach (var target in _targets)
            {
                var summary = await target.ScanAsync(CancellationToken.None);
                var selectable = summary.CanClean && (!summary.RequiresElevation || _isElevated);
                results.Add(new CleanerRowState(target, summary, selectable));

                var logStatus = summary.RequiresElevation && !_isElevated && summary.CanClean
                    ? $"{summary.Status} Run as administrator to clean this category."
                    : summary.Status;
                AppendLog($"{summary.DisplayName}: {logStatus} ({Helpers.ByteSizeFormatter.Format(summary.ReclaimableBytes)}).");
            }

            PopulateCleanerRows(results);
            UpdateSelectionSummary();

            var totalBytes = results.Sum(item => item.Summary.ReclaimableBytes);
            StatusLabel.Text = $"Scan complete. {Helpers.ByteSizeFormatter.Format(totalBytes)} available to reclaim.";
        }
        catch (Exception exception)
        {
            AppendLog($"Scan failed: {exception.Message}");
            StatusLabel.Text = "Scan failed.";
        }
        finally
        {
            if (ownsCleanupOverlay)
            {
                _cleanupPage.HideBusyOverlay();
            }

            SetBusyState(false);
        }
    }

    private async Task RunCleanAsync()
    {
        if (_isBusy)
        {
            return;
        }

        var selectedRows = GetSelectedCleanerRows().ToList();
        if (selectedRows.Count == 0)
        {
            StatusLabel.Text = "Nothing selected to clean.";
            return;
        }

        var selectedBytes = selectedRows.Sum(item => item.Summary.ReclaimableBytes);
        var confirmation = MessageBox.Show(
            this,
            $"Delete the selected cleanup targets now?\r\n\r\nSelected size: {Helpers.ByteSizeFormatter.Format(selectedBytes)}",
            "Confirm cleanup",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Warning);

        if (confirmation != MessageBoxResult.OK)
        {
            return;
        }

        _cleanupPage.ShowBusyOverlay("Cleaning...");
        SetBusyState(true, "Cleaning selected targets...");
        AppendLog($"Starting cleanup for {selectedRows.Count} selected categories.");

        try
        {
            long removedBytes = 0;
            var removedItems = 0;

            foreach (var row in selectedRows)
            {
                var result = await row.Target.CleanAsync(CancellationToken.None);
                removedBytes += result.DeletedBytes;
                removedItems += result.DeletedItems;
                AppendLog($"{result.DisplayName}: {result.Message}");
            }

            _homePage.RecordCleanupStats(removedBytes);
            if (Application.Current is App app)
            {
                app.ShowTrayNotification(
                    "Cleanup complete",
                    $"Freed {Helpers.ByteSizeFormatter.Format(removedBytes)}. Your system is cleaner.");
            }

            StatusLabel.Text = $"Cleanup complete. Removed {removedItems:N0} items ({Helpers.ByteSizeFormatter.Format(removedBytes)}).";
            await RefreshScanResultsAsync();
            NavigateTo(AppPage.Activity);
        }
        catch (Exception exception)
        {
            AppendLog($"Cleanup failed: {exception.Message}");
            StatusLabel.Text = "Cleanup failed.";
        }
        finally
        {
            _cleanupPage.HideBusyOverlay();
            SetBusyState(false);
        }
    }

    private async Task RunLargeFileAnalysisAsync()
    {
        if (_isBusy)
        {
            return;
        }

        var rootPath = _largeFilesPage.FolderPath.Text.Trim();
        if (!Directory.Exists(rootPath))
        {
            StatusLabel.Text = "Choose an existing folder first.";
            return;
        }

        if (!int.TryParse(_largeFilesPage.MinimumSize.Text.Trim(), out var minimumMb) || minimumMb < 16)
        {
            StatusLabel.Text = "Minimum size must be a number >= 16 MB.";
            return;
        }

        SetBusyState(true, "Analyzing large files...");
        AppendLog($"Analyzing large files in {rootPath}.");

        try
        {
            var minimumBytes = minimumMb * 1024L * 1024L;
            var result = await _largeFileAnalyzer.AnalyzeAsync(rootPath, 150, minimumBytes, CancellationToken.None);

            _largeFileRows.Clear();
            foreach (var file in result.LargestFiles)
            {
                _largeFileRows.Add(new LargeFileRowViewModel(file));
            }

            _largeFilesPage.Summary.Text = $"{result.LargestFiles.Count:N0} large files found from {result.ScannedFileCount:N0} scanned files. Skipped {result.SkippedDirectoryCount:N0} restricted folders.";
            StatusLabel.Text = $"Analyzer complete. Showing {result.LargestFiles.Count:N0} files.";
            AppendLog($"Analyzer finished: {result.LargestFiles.Count:N0} files above {minimumMb:N0} MB.");
        }
        catch (Exception exception)
        {
            AppendLog($"Analyzer failed: {exception.Message}");
            StatusLabel.Text = "Analyzer failed.";
        }
        finally
        {
            SetBusyState(false);
        }
    }

    private void PopulateCleanerRows(IEnumerable<CleanerRowState> rows)
    {
        foreach (var existing in _cleanupRows)
        {
            existing.PropertyChanged -= CleanupRowViewModel_OnPropertyChanged;
        }

        _cleanupRows.Clear();

        foreach (var rowState in rows)
        {
            var summary = rowState.Summary;
            var displayStatus = summary.RequiresElevation && !_isElevated && summary.CanClean
                ? "Run as administrator to clean."
                : summary.Status;

            var rowView = new CleanupRowViewModel(rowState, displayStatus);
            rowView.PropertyChanged += CleanupRowViewModel_OnPropertyChanged;
            _cleanupRows.Add(rowView);
        }
    }

    private IEnumerable<CleanerRowState> GetSelectedCleanerRows()
    {
        return _cleanupRows
            .Where(row => row.IsSelectable && row.IsSelected)
            .Select(row => row.State);
    }

    private void UpdateSelectionSummary()
    {
        long totalAvailable = 0;
        long totalSelected = 0;
        var readyCount = 0;
        var selectableCount = 0;
        var selectedCount = 0;

        foreach (var row in _cleanupRows)
        {
            totalAvailable += row.State.Summary.ReclaimableBytes;
            if (row.State.Summary.CanClean)
            {
                readyCount++;
            }

            if (row.IsSelectable)
            {
                selectableCount++;
                if (row.IsSelected)
                {
                    totalSelected += row.State.Summary.ReclaimableBytes;
                    selectedCount++;
                }
            }
        }

        _homePage.ReclaimableValue.Text = Helpers.ByteSizeFormatter.Format(totalAvailable);
        _homePage.SelectedValue.Text = Helpers.ByteSizeFormatter.Format(totalSelected);
        _homePage.ReadyValue.Text = readyCount.ToString("N0");

        _cleanupPage.AvailableValue.Text = Helpers.ByteSizeFormatter.Format(totalAvailable);
        _cleanupPage.SelectedValue.Text = Helpers.ByteSizeFormatter.Format(totalSelected);
        _cleanupPage.ReadyValue.Text = readyCount.ToString("N0");

        _homePage.UpdateProgressBars(totalSelected, totalAvailable, readyCount, _targets.Count);
        _homePage.SelectionInfo.Text = $"Selection progress: {Helpers.ByteSizeFormatter.Format(totalSelected)} selected";
        _homePage.ReadyInfo.Text = $"Categories ready: {readyCount:N0} of {_targets.Count:N0}";

        _isUpdatingSelection = true;
        try
        {
            _cleanupPage.SelectAll.IsChecked = selectableCount > 0 && selectedCount == selectableCount;
        }
        finally
        {
            _isUpdatingSelection = false;
        }

        UpdateLogViews();
    }

    private void UpdateLogViews()
    {
        var text = _logEntries.Count == 0 ? "No activity yet." : string.Join(Environment.NewLine, _logEntries);
        _activityPage.SetLogText(text);

        var previewEntries = _logEntries.Count <= 8
            ? _logEntries
            : _logEntries.Skip(Math.Max(0, _logEntries.Count - 8)).ToList();
        _homePage.SetRecentActivityText(previewEntries.Count == 0 ? "No activity yet." : string.Join(Environment.NewLine, previewEntries));

        SidebarLogCountText.Text = $"{_logEntries.Count:N0} log entries";
    }

    private void SetBusyState(bool busy, string? statusText = null)
    {
        _isBusy = busy;

        _homePage.ScanButton.IsEnabled = !busy;
        _homePage.CleanupButton.IsEnabled = !busy;
        _cleanupPage.ScanActionButton.IsEnabled = !busy;
        _cleanupPage.CleanActionButton.IsEnabled = !busy;
        _cleanupPage.ElevateActionButton.IsEnabled = !busy;
        _cleanupPage.SelectAll.IsEnabled = !busy;
        _largeFilesPage.BrowseActionButton.IsEnabled = !busy;
        _largeFilesPage.AnalyzeActionButton.IsEnabled = !busy;

        foreach (var button in _navigationButtons.Values)
        {
            button.IsEnabled = !busy;
        }

        Mouse.OverrideCursor = busy ? Cursors.Wait : null;
        if (!string.IsNullOrWhiteSpace(statusText))
        {
            StatusLabel.Text = statusText;
        }
    }

    private void AppendLog(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logEntries.Add(entry);

        if (_logEntries.Count > 300)
        {
            _logEntries.RemoveAt(0);
        }

        UpdateLogViews();
    }

    private async Task RefreshScanResultsAsync()
    {
        var wasBusy = _isBusy;
        _isBusy = false;
        try
        {
            await RunScanAsync();
        }
        finally
        {
            _isBusy = wasBusy;
        }
    }

    private void SelectAllCheckBox_OnChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        var isChecked = _cleanupPage.SelectAll.IsChecked == true;
        foreach (var row in _cleanupRows)
        {
            if (row.IsSelectable)
            {
                row.IsSelected = isChecked;
            }
        }

        UpdateSelectionSummary();
    }

    private void CleanupRowViewModel_OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(CleanupRowViewModel.IsSelected))
        {
            UpdateSelectionSummary();
        }
    }

    private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select a folder to analyze",
            InitialDirectory = Directory.Exists(_largeFilesPage.FolderPath.Text)
                ? _largeFilesPage.FolderPath.Text
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        if (dialog.ShowDialog() == true)
        {
            _largeFilesPage.FolderPath.Text = dialog.FolderName;
        }
    }

    private void LargeFilesResults_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (_largeFilesPage.Results.SelectedItem is not LargeFileRowViewModel row)
        {
            return;
        }

        if (!File.Exists(row.Record.FullPath))
        {
            StatusLabel.Text = "The file no longer exists.";
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{row.Record.FullPath}\"",
            UseShellExecute = true
        });
    }

    private static bool IsProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void ShowAndActivate()
    {
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;

        if (!IsVisible)
        {
            Show();
        }

        WindowState = WindowState.Normal;
        Activate();
        Focus();
    }

    public async void RunScanFromTray()
    {
        ShowAndActivate();
        NavigateTo(AppPage.Cleanup);
        await RunScanAsync();
    }

    public void AllowCloseFromTray()
    {
        _allowClose = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_allowClose)
        {
            base.OnClosing(e);
            return;
        }

        e.Cancel = true;
        BeginAnimation(OpacityProperty, null);
        Opacity = 1;
        Hide();

        if (Application.Current is App app)
        {
            app.ShowTrayNotification(
                "SonyDev Cleaner",
                "Running in background. Double-click tray icon to open.");
        }
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.Home);

    private void CleanupNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.Cleanup);

    private void LargeFilesNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.LargeFiles);

    private void ActivityNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.Activity);

    private void StartupNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.Startup);

    private void DiskUsageNavButton_Click(object sender, RoutedEventArgs e) => NavigateTo(AppPage.DiskUsage);

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            ToggleMaximize();
            return;
        }

        DragMove();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        var animation = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(180));
        animation.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, animation);
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnMaximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void ToggleMaximize()
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private sealed record CleanerRowState(CleanerTarget Target, CleanerScanSummary Summary, bool Selectable);

    private sealed class CleanupRowViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public CleanupRowViewModel(CleanerRowState state, string displayStatus)
        {
            State = state;
            Category = state.Summary.DisplayName;
            Description = state.Summary.Description;
            Size = Helpers.ByteSizeFormatter.Format(state.Summary.ReclaimableBytes);
            Files = state.Summary.ItemCount.ToString("N0");
            Status = displayStatus;
            IsSelectable = state.Selectable;
            _isSelected = state.Selectable;
        }

        public CleanerRowState State { get; }

        public string Category { get; }

        public string Description { get; }

        public string Size { get; }

        public string Files { get; }

        public string Status { get; }

        public bool IsSelectable { get; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    private sealed class LargeFileRowViewModel
    {
        public LargeFileRowViewModel(LargeFileRecord record)
        {
            Record = record;
            Name = Path.GetFileName(record.FullPath);
            Folder = Path.GetDirectoryName(record.FullPath) ?? string.Empty;
            Size = Helpers.ByteSizeFormatter.Format(record.SizeBytes);
            Modified = record.LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
        }

        public LargeFileRecord Record { get; }

        public string Name { get; }

        public string Folder { get; }

        public string Size { get; }

        public string Modified { get; }
    }
}
