using Hardcodet.Wpf.TaskbarNotification;
using SonyDevCleaner.App.Services;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using WinForms = System.Windows.Forms;

namespace SonyDevCleaner.App;

public partial class App : Application
{
    public TaskbarIcon TrayIcon { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        if (e.Args.Contains("--background-scan", StringComparer.OrdinalIgnoreCase))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            RunBackgroundScan();
            return;
        }

        TrayIcon = new TaskbarIcon
        {
            ToolTipText = "SonyDev Cleaner",
            Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location)
                ?? SystemIcons.Application,
            ContextMenu = BuildTrayMenu()
        };

        TrayIcon.TrayMouseDoubleClick += TrayIcon_DoubleClick;

        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        TrayIcon?.Dispose();
        base.OnExit(e);
    }

    public void ShowTrayNotification(string title, string message, BalloonIcon icon = BalloonIcon.Info)
    {
        TrayIcon?.ShowBalloonTip(title, message, icon);
    }

    private ContextMenu BuildTrayMenu()
    {
        var menu = new ContextMenu();

        var openItem = new MenuItem { Header = "Open" };
        openItem.Click += TrayMenu_Open;
        menu.Items.Add(openItem);

        var scanItem = new MenuItem { Header = "Run Scan" };
        scanItem.Click += TrayMenu_Scan;
        menu.Items.Add(scanItem);

        menu.Items.Add(new Separator());

        var exitItem = new MenuItem { Header = "Exit" };
        exitItem.Click += TrayMenu_Exit;
        menu.Items.Add(exitItem);

        return menu;
    }

    private void TrayIcon_DoubleClick(object? sender, RoutedEventArgs e)
        => (MainWindow as MainWindow)?.ShowAndActivate();

    private void TrayMenu_Open(object? sender, RoutedEventArgs e)
        => (MainWindow as MainWindow)?.ShowAndActivate();

    private void TrayMenu_Scan(object? sender, RoutedEventArgs e)
        => (MainWindow as MainWindow)?.RunScanFromTray();

    private void TrayMenu_Exit(object? sender, RoutedEventArgs e)
    {
        if (MainWindow is MainWindow mainWindow)
        {
            mainWindow.AllowCloseFromTray();
        }

        Shutdown();
    }

    private async void RunBackgroundScan()
    {
        long totalBytes = 0;

        try
        {
            var targets = CleanerCatalog.CreateDefaultTargets();

            foreach (var target in targets)
            {
                try
                {
                    var summary = await target.ScanAsync(CancellationToken.None);
                    totalBytes += summary.ReclaimableBytes;
                }
                catch
                {
                }
            }

            using var notifyIcon = new WinForms.NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Information,
                BalloonTipTitle = "SonyDev Cleaner",
                BalloonTipText = totalBytes > 0
                    ? $"Weekly scan: {Helpers.ByteSizeFormatter.Format(totalBytes)} available to reclaim."
                    : "Weekly scan complete. System is clean.",
                BalloonTipIcon = WinForms.ToolTipIcon.Info
            };

            notifyIcon.ShowBalloonTip(6000);
            await Task.Delay(7000);
        }
        finally
        {
            Shutdown();
        }
    }
}
