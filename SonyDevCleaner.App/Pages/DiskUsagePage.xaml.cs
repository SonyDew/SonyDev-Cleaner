using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SonyDevCleaner.App.Pages;

public partial class DiskUsagePage : UserControl
{
    private readonly ObservableCollection<DiskFolderItem> _topFolders = [];

    public DiskUsagePage()
    {
        InitializeComponent();
        TopFoldersList.ItemsSource = _topFolders;
        Loaded += DiskUsagePage_Loaded;
    }

    private void DiskUsagePage_Loaded(object sender, RoutedEventArgs e)
    {
        PopulateDrives();
    }

    private void PopulateDrives()
    {
        var selectedDrive = DriveCombo.SelectedItem as string;
        var drives = DriveInfo
            .GetDrives()
            .Where(drive => drive.DriveType == DriveType.Fixed && drive.IsReady)
            .Select(drive => drive.RootDirectory.FullName)
            .ToList();

        DriveCombo.ItemsSource = drives;
        if (drives.Count == 0)
        {
            DriveCombo.SelectedItem = null;
            DiskUsageScanStatus.Text = "No fixed drives available";
            return;
        }

        DriveCombo.SelectedItem = drives.Contains(selectedDrive, StringComparer.OrdinalIgnoreCase)
            ? selectedDrive
            : drives[0];

        if (string.IsNullOrWhiteSpace(DiskUsageScanStatus.Text))
        {
            DiskUsageScanStatus.Text = "Select a drive and click Analyze";
        }
    }

    private void DriveCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        FolderBars.Children.Clear();
        _topFolders.Clear();
        DiskUsageScanStatus.Text = DriveCombo.SelectedItem is string drive
            ? $"Ready to scan {drive}"
            : "Select a drive";
    }

    private async void Analyze_Click(object sender, RoutedEventArgs e)
    {
        if (DriveCombo.SelectedItem is not string drive || string.IsNullOrWhiteSpace(drive))
        {
            DiskUsageScanStatus.Text = "Select a drive first";
            return;
        }

        AnalyzeButton.IsEnabled = false;
        DiskUsageScanStatus.Text = "Scanning...";

        try
        {
            var results = await Task.Run(() => GetTopFolders(drive, 30));
            PopulateFolderBars(results);
            PopulateTopFolders(results.Take(15));
            DiskUsageScanStatus.Text = $"Done — {results.Count} folders";
        }
        finally
        {
            AnalyzeButton.IsEnabled = true;
        }
    }

    private void PopulateTopFolders(IEnumerable<(string Path, long Size)> folders)
    {
        _topFolders.Clear();
        foreach (var folder in folders)
        {
            _topFolders.Add(new DiskFolderItem
            {
                Path = folder.Path,
                Name = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(folder.Path)),
                Size = folder.Size
            });
        }
    }

    private void PopulateFolderBars(IReadOnlyList<(string Path, long Size)> folders)
    {
        FolderBars.Children.Clear();
        if (folders.Count == 0)
        {
            FolderBars.Children.Add(new TextBlock
            {
                Text = "No folders found or access was denied.",
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#88FFFFFF")),
                FontSize = 12
            });
            return;
        }

        FolderBarsScrollViewer.UpdateLayout();
        var maxSize = Math.Max(1, folders.Max(folder => folder.Size));
        var maxBarWidth = Math.Max(180, FolderBarsScrollViewer.ViewportWidth - 320);

        foreach (var folder in folders)
        {
            var row = new Grid
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var nameText = new TextBlock
            {
                Text = System.IO.Path.GetFileName(System.IO.Path.TrimEndingDirectorySeparator(folder.Path)),
                Foreground = Brushes.White,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = folder.Path
            };
            Grid.SetColumn(nameText, 0);

            var barHost = new Grid
            {
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(barHost, 1);

            var track = new Border
            {
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15FFFFFF")),
                Height = 22
            };

            var fill = new Border
            {
                CornerRadius = new CornerRadius(4),
                Height = 22,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = maxBarWidth * (folder.Size / (double)maxSize),
                Background = new LinearGradientBrush(
                    (Color)ColorConverter.ConvertFromString("#FF5E8AFF"),
                    (Color)ColorConverter.ConvertFromString("#FFA855F7"),
                    0)
            };

            barHost.Children.Add(track);
            barHost.Children.Add(fill);

            var sizeText = new TextBlock
            {
                Text = FormatBytes(folder.Size),
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#88FFFFFF")),
                FontSize = 10,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(sizeText, 2);

            row.Children.Add(nameText);
            row.Children.Add(barHost);
            row.Children.Add(sizeText);
            FolderBars.Children.Add(row);
        }
    }

    private static List<(string Path, long Size)> GetTopFolders(string root, int top)
    {
        var results = new List<(string Path, long Size)>();

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                try
                {
                    var size = GetDirectorySize(directory);
                    results.Add((directory, size));
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return results
            .OrderByDescending(item => item.Size)
            .Take(top)
            .ToList();
    }

    private static long GetDirectorySize(string path)
    {
        long size = 0;

        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try
                {
                    size += new FileInfo(file).Length;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return size;
    }

    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
            >= 1_048_576 => $"{bytes / 1_048_576.0:F0} MB",
            >= 1_024 => $"{bytes / 1_024.0:F0} KB",
            _ => $"{bytes} B"
        };
    }

    private sealed class DiskFolderItem
    {
        public string Path { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public long Size { get; set; }

        public string SizeText => FormatBytes(Size);
    }
}
