using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SonyDevCleaner.App.Services;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using System.Windows.Threading;

namespace SonyDevCleaner.App.Pages;

public partial class HomePage : UserControl
{
    [DllImport("Shell32.dll")]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);

    private const uint SherbNoConfirmation = 0x00000001;
    private const uint SherbNoProgressUi = 0x00000002;
    private const uint SherbNoSound = 0x00000004;

    private readonly StatsService _statsService = new();
    private readonly ScheduleService _scheduleService = new();
    private double _selectedBytes;
    private double _totalBytes;
    private int _readyCount;
    private int _totalCount;

    public HomePage()
    {
        InitializeComponent();
        Loaded += (_, _) =>
        {
            ApplyProgressBars();
            RefreshStats();
        };
        SizeChanged += (_, _) => ApplyProgressBars();
    }

    public TextBlock ReclaimableValue => ReclaimableValueText;

    public TextBlock SelectedValue => SelectedValueText;

    public TextBlock ReadyValue => ReadyValueText;

    public TextBlock ModeValue => ModeValueText;

    public Button ScanButton => RunScanButton;

    public Button CleanupButton => OpenCleanupButton;

    public RichTextBox RecentActivityLog => RecentActivityTextBox;

    public TextBlock SelectionInfo => SelectionInfoText;

    public TextBlock ReadyInfo => ReadyInfoText;

    public void UpdateProgressBars(double selectedBytes, double totalBytes, int readyCount, int totalCount)
    {
        _selectedBytes = selectedBytes;
        _totalBytes = totalBytes;
        _readyCount = readyCount;
        _totalCount = totalCount;

        Dispatcher.BeginInvoke(ApplyProgressBars, DispatcherPriority.Loaded);
    }

    private void ApplyProgressBars()
    {
        SelectionFill.Width = SelectionFill.Parent is Grid g
            ? Math.Max(0, g.ActualWidth * (_selectedBytes / Math.Max(1, _totalBytes)))
            : 0;

        CategoriesFill.Width = CategoriesFill.Parent is Grid g2
            ? Math.Max(0, g2.ActualWidth * (_readyCount / Math.Max(1.0, _totalCount)))
            : 0;
    }

    public void SetRecentActivityText(string text)
    {
        RecentActivityTextBox.Document = CreateDocument(text);
        RecentActivityTextBox.ScrollToEnd();
    }

    public void RefreshStats()
    {
        var stats = _statsService.Load();
        StatMonthLabel.Text = FormatBytes(stats.BytesCleanedThisMonth);
        StatTotalLabel.Text = FormatBytes(stats.TotalBytesCleanedAllTime);
        StatRunsLabel.Text = stats.CleanRunsThisMonth.ToString();
        StatLastLabel.Text = stats.LastCleanDate == default
            ? "Never"
            : stats.LastCleanDate.ToString("dd MMM");
        RefreshScheduleStatus();
    }

    public void RecordCleanupStats(long bytesFreed)
    {
        _statsService.RecordClean(bytesFreed);
        RefreshStats();
    }

    private async void FlushDns_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            FlushDnsButton.IsEnabled = false;
            var process = await Task.Run(() => Process.Start(new ProcessStartInfo("ipconfig", "/flushdns")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            }));

            process?.WaitForExit();
            FlushDnsButton.Content = "✓ DNS Flushed";
            await Task.Delay(2000);
            ResetFlushDnsButtonContent();
        }
        catch
        {
            FlushDnsButton.Content = "⚠ Failed";
            await Task.Delay(2000);
            ResetFlushDnsButtonContent();
        }
        finally
        {
            FlushDnsButton.IsEnabled = true;
        }
    }

    private async void EmptyBin_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            EmptyBinButton.IsEnabled = false;
            EmptyBinButton.Content = "Emptying...";
            await Task.Run(() => SHEmptyRecycleBin(
                IntPtr.Zero,
                null,
                SherbNoConfirmation | SherbNoProgressUi | SherbNoSound));
            EmptyBinButton.Content = "✓ Done";
            await Task.Delay(2000);
            ResetEmptyBinButtonContent();
            RefreshStats();
        }
        catch
        {
            EmptyBinButton.Content = "⚠ Failed";
            await Task.Delay(2000);
            ResetEmptyBinButtonContent();
        }
        finally
        {
            EmptyBinButton.IsEnabled = true;
        }
    }

    private void ScheduleToggle_Click(object sender, RoutedEventArgs e)
    {
        var active = _scheduleService.IsScheduled();
        var ok = active
            ? _scheduleService.Disable()
            : _scheduleService.Enable();

        if (!ok && !active)
        {
            ScheduleStatusLabel.Text = "Failed — try running as Administrator";
            return;
        }

        RefreshScheduleStatus();
    }

    private void RefreshScheduleStatus()
    {
        var active = _scheduleService.IsScheduled();
        ScheduleStatusLabel.Text = active
            ? "Runs every Sunday at 10:00 AM"
            : "Not scheduled";
        ScheduleToggleButton.Content = active ? "Disable" : "Enable";
        ScheduleToggleButton.Style = (Style)FindResource(active ? "AccentButton" : "GlassButton");
    }

    private void ResetFlushDnsButtonContent()
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new System.Windows.Controls.TextBlock { Text = "🌐", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new System.Windows.Controls.TextBlock { Text = "Flush DNS Cache", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        FlushDnsButton.Content = content;
    }

    private void ResetEmptyBinButtonContent()
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        content.Children.Add(new System.Windows.Controls.TextBlock { Text = "🗑", FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        content.Children.Add(new System.Windows.Controls.TextBlock { Text = "Empty Recycle Bin", Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        EmptyBinButton.Content = content;
    }

    private static FlowDocument CreateDocument(string text)
    {
        var paragraph = new Paragraph(new Run(text)) { Margin = new System.Windows.Thickness(0) };
        return new FlowDocument(paragraph) { PagePadding = new System.Windows.Thickness(0) };
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        >= 1_073_741_824 => $"{bytes / 1_073_741_824.0:F1} GB",
        >= 1_048_576 => $"{bytes / 1_048_576.0:F0} MB",
        >= 1_024 => $"{bytes / 1_024.0:F0} KB",
        _ => $"{bytes} B"
    };
}
