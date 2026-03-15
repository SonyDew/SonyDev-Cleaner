using System.Runtime.InteropServices;

namespace SonyDevCleaner.App.Helpers;

internal static class WindowBackdrop
{
    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmsbtTransientWindow = 3;
    private const int DwmwcpRound = 2;

    public static void TryApply(IntPtr handle)
    {
        if (handle == IntPtr.Zero || !OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        var roundedCorners = DwmwcpRound;
        _ = DwmSetWindowAttribute(
            handle,
            DwmwaWindowCornerPreference,
            ref roundedCorners,
            Marshal.SizeOf<int>());

        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621))
        {
            return;
        }

        var backdropType = DwmsbtTransientWindow;
        _ = DwmSetWindowAttribute(
            handle,
            DwmwaSystemBackdropType,
            ref backdropType,
            Marshal.SizeOf<int>());
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int dwAttribute,
        ref int pvAttribute,
        int cbAttribute);
}
