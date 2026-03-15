using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Win32;

namespace SonyDevCleaner.App.Pages;

public partial class CleanupPage : UserControl
{
    private static readonly Dictionary<string, string> CategoryGroups = new(StringComparer.OrdinalIgnoreCase)
    {
        ["User temp"] = "System",
        ["Windows temp"] = "System",
        ["Thumbnail cache"] = "System",
        ["Font cache"] = "System",
        ["Windows Update cache"] = "System",
        ["Log files"] = "System",
        ["Prefetch files"] = "System",
        ["DirectX shader cache"] = "System",
        ["Crash dumps"] = "System",
        ["Windows Error Reporting"] = "System",
        ["Empty folders"] = "System",
        ["Recycle Bin"] = "System",
        ["Chrome cache"] = "Browsers",
        ["Edge cache"] = "Browsers",
        ["Firefox cache"] = "Browsers",
        ["Discord cache"] = "Apps",
        ["Steam download cache"] = "Apps",
        ["Teams cache"] = "Apps",
        ["OneDrive cache"] = "Apps",
        ["Spotify cache"] = "Apps",
        ["Visual Studio cache"] = "Developer",
        ["VS Code cache"] = "Developer",
        ["npm cache"] = "Developer",
        ["pip cache"] = "Developer",
        ["NuGet cache"] = "Developer"
    };

    private string _activeFilter = "All";
    private ICollectionView? _rowsView;
    private INotifyCollectionChanged? _rowsCollectionNotifier;
    private List<object> _allRows = [];

    public CleanupPage()
    {
        InitializeComponent();
        Loaded += CleanupPage_Loaded;
    }

    public Button ScanActionButton => ScanButton;

    public Button CleanActionButton => CleanButton;

    public Button ElevateActionButton => ElevateButton;

    public Button ExportActionButton => ExportButton;

    public CheckBox SelectAll => SelectAllCheckBox;

    public TextBlock AvailableValue => AvailableValueText;

    public TextBlock SelectedValue => SelectedValueText;

    public TextBlock ReadyValue => ReadyValueText;

    public DataGrid Grid => CleanupGrid;

    public bool IsBusyOverlayVisible => BusyOverlay.Visibility == Visibility.Visible;

    public void ShowBusyOverlay(string text)
    {
        BusyLabel.Text = text;
        BusyOverlay.Visibility = Visibility.Visible;
    }

    public void HideBusyOverlay()
    {
        BusyOverlay.Visibility = Visibility.Collapsed;
    }

    private void CleanupPage_Loaded(object sender, RoutedEventArgs e)
    {
        var isAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent())
            .IsInRole(WindowsBuiltInRole.Administrator);
        ElevateButton.Visibility = isAdmin ? Visibility.Collapsed : Visibility.Visible;
        InitializeRowsView();
        UpdateFilterButtonStyles(_activeFilter);
        UpdateExportAvailability();
    }

    private void ElevateButton_Click(object sender, RoutedEventArgs e)
    {
        var psi = new ProcessStartInfo
        {
            FileName = Environment.ProcessPath ??
                       Assembly.GetExecutingAssembly().Location,
            UseShellExecute = true,
            Verb = "runas"
        };

        try
        {
            Process.Start(psi);
            Application.Current.Shutdown();
        }
        catch (Exception)
        {
        }
    }

    private void Filter_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        _activeFilter = button.Tag?.ToString() ?? "All";
        UpdateFilterButtonStyles(_activeFilter);
        ApplyFilter();
    }

    private void InitializeRowsView()
    {
        if (CleanupGrid.ItemsSource is null)
        {
            UpdateExportAvailability();
            return;
        }

        if (!ReferenceEquals(_rowsCollectionNotifier, CleanupGrid.ItemsSource))
        {
            if (_rowsCollectionNotifier is not null)
            {
                _rowsCollectionNotifier.CollectionChanged -= RowsCollectionChanged;
            }

            _rowsCollectionNotifier = CleanupGrid.ItemsSource as INotifyCollectionChanged;
            if (_rowsCollectionNotifier is not null)
            {
                _rowsCollectionNotifier.CollectionChanged += RowsCollectionChanged;
            }
        }

        _rowsView = CollectionViewSource.GetDefaultView(CleanupGrid.ItemsSource);
        if (_rowsView is CollectionView collectionView)
        {
            collectionView.GroupDescriptions.Clear();
            collectionView.GroupDescriptions.Add(new PropertyGroupDescription("Category", new CategoryGroupConverter()));
        }

        CaptureAllRows();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (_rowsView is null)
        {
            return;
        }

        _rowsView.Filter = item => MatchesFilter(item, _activeFilter);
        _rowsView.Refresh();
        UpdateExportAvailability();
    }

    private void UpdateFilterButtonStyles(string activeFilter)
    {
        foreach (var button in new[] { FilterAll, FilterReady, FilterRestricted, FilterNothing })
        {
            button.Style = (Style)FindResource("GlassButton");
        }

        var activeButton = activeFilter switch
        {
            "Ready" => FilterReady,
            "Restricted" => FilterRestricted,
            "Nothing" => FilterNothing,
            _ => FilterAll
        };

        activeButton.Style = (Style)FindResource("AccentButton");
    }

    private static bool MatchesFilter(object item, string filter)
    {
        var status = GetStringProperty(item, "Status");

        return filter switch
        {
            "Ready" => status.StartsWith("Ready to clean", StringComparison.OrdinalIgnoreCase),
            "Restricted" => status.Contains("restricted", StringComparison.OrdinalIgnoreCase)
                || status.Contains("administrator", StringComparison.OrdinalIgnoreCase),
            "Nothing" => status.StartsWith("Nothing to clean", StringComparison.OrdinalIgnoreCase),
            _ => true
        };
    }

    private static string GetStringProperty(object item, string propertyName)
    {
        var property = TypeDescriptor.GetProperties(item)[propertyName];
        var value = property?.GetValue(item);
        return value?.ToString() ?? string.Empty;
    }

    private async void Export_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Save Scan Report",
            Filter = "Text file (*.txt)|*.txt",
            FileName = $"SonyDevCleaner_Report_{DateTime.Now:yyyy-MM-dd_HH-mm}"
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var reportRows = _allRows.Count == 0
            ? GetCurrentRows().ToList()
            : _allRows.ToList();

        var builder = new System.Text.StringBuilder();
        builder.AppendLine("═══════════════════════════════════════════");
        builder.AppendLine("  SonyDev Cleaner — Scan Report");
        builder.AppendLine($"  Generated: {DateTime.Now:dd MMM yyyy, HH:mm:ss}");
        builder.AppendLine($"  Mode: {(IsElevated() ? "Administrator" : "Standard")}");
        builder.AppendLine("═══════════════════════════════════════════");
        builder.AppendLine();

        var groupOrder = new[] { "System", "Browsers", "Apps", "Developer", "Other" };
        var groups = reportRows
            .GroupBy(row =>
            {
                var category = GetStringProperty(row, "Category");
                return CategoryGroups.TryGetValue(category, out var group) ? group : "Other";
            })
            .OrderBy(group => Array.IndexOf(groupOrder, group.Key) is var index && index >= 0 ? index : int.MaxValue)
            .ThenBy(group => group.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            builder.AppendLine($"── {group.Key} ──────────────────────────");
            foreach (var row in group)
            {
                var category = GetStringProperty(row, "Category");
                var size = FormatBytes(GetBytes(row));
                var files = GetFileCount(row);
                var status = GetStringProperty(row, "Status");
                builder.AppendLine($"  {category,-28} {size,10}   {files,5:N0} files   {status}");
            }

            builder.AppendLine();
        }

        builder.AppendLine("═══════════════════════════════════════════");
        var total = reportRows.Sum(GetBytes);
        builder.AppendLine($"  TOTAL RECLAIMABLE: {FormatBytes(total)}");
        builder.AppendLine("═══════════════════════════════════════════");

        File.WriteAllText(dialog.FileName, builder.ToString());

        ExportButton.IsEnabled = false;
        ExportButton.Content = "✓ Saved";
        await Task.Delay(2000);
        ResetExportButtonContent();
        UpdateExportAvailability();
    }

    private void RowsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        CaptureAllRows();
        UpdateExportAvailability();
    }

    private void CaptureAllRows()
    {
        _allRows = GetCurrentRows().ToList();
    }

    private IEnumerable<object> GetCurrentRows()
    {
        return CleanupGrid.ItemsSource is IEnumerable items
            ? items.Cast<object>()
            : Enumerable.Empty<object>();
    }

    private void UpdateExportAvailability()
    {
        ExportButton.IsEnabled = _allRows.Count > 0;
    }

    private void ResetExportButtonContent()
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new TextBlock { Text = "↓", FontSize = 14, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new TextBlock { Text = "Export Report", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        ExportButton.Content = content;
    }

    private static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    private static long GetBytes(object row)
    {
        var stateProperty = row.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
        var state = stateProperty?.GetValue(row);
        var summary = state?.GetType().GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public)?.GetValue(state);
        var bytesValue = summary?.GetType().GetProperty("ReclaimableBytes", BindingFlags.Instance | BindingFlags.Public)?.GetValue(summary);

        return bytesValue switch
        {
            long bytes => bytes,
            int bytes => bytes,
            _ => 0
        };
    }

    private static int GetFileCount(object row)
    {
        var stateProperty = row.GetType().GetProperty("State", BindingFlags.Instance | BindingFlags.Public);
        var state = stateProperty?.GetValue(row);
        var summary = state?.GetType().GetProperty("Summary", BindingFlags.Instance | BindingFlags.Public)?.GetValue(state);
        var countValue = summary?.GetType().GetProperty("ItemCount", BindingFlags.Instance | BindingFlags.Public)?.GetValue(summary);

        return countValue switch
        {
            int count => count,
            long count => (int)count,
            _ => 0
        };
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F0} MB",
        >= 1_024 => $"{bytes / 1_024.0:F0} KB",
        _ => $"{bytes} B"
    };

    private sealed class CategoryGroupConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var category = value?.ToString() ?? string.Empty;
            return CategoryGroups.TryGetValue(category, out var group) ? group : "Other";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
