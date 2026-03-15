using System.Runtime.InteropServices;
using SonyDevCleaner.App.Models;

namespace SonyDevCleaner.App.Services;

public sealed class RecycleBinCleanerTarget : CleanerTarget
{
    public RecycleBinCleanerTarget()
        : base(
            "recycle-bin",
            "Recycle Bin",
            "Items already marked for deletion. This uses the Windows shell API.")
    {
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
        cancellationToken.ThrowIfCancellationRequested();

        var info = new SHQUERYRBINFO
        {
            cbSize = (uint)Marshal.SizeOf<SHQUERYRBINFO>()
        };

        var result = SHQueryRecycleBin(null, ref info);
        if (result != 0)
        {
            return new CleanerScanSummary(
                Id,
                DisplayName,
                Description,
                0,
                0,
                $"Windows API error 0x{unchecked((uint)result):X8}.",
                false,
                false);
        }

        return new CleanerScanSummary(
            Id,
            DisplayName,
            Description,
            info.i64Size,
            (int)Math.Min(info.i64NumItems, int.MaxValue),
            info.i64NumItems == 0 ? "Nothing to clean." : "Ready to clean.",
            info.i64NumItems > 0,
            false);
    }

    private CleanerExecutionResult CleanInternal(CancellationToken cancellationToken)
    {
        var summary = ScanInternal(cancellationToken);
        if (!summary.CanClean)
        {
            return new CleanerExecutionResult(Id, DisplayName, 0, 0, 0, summary.Status);
        }

        var result = SHEmptyRecycleBin(
            IntPtr.Zero,
            null,
            RecycleBinFlags.NoConfirmation | RecycleBinFlags.NoProgressUi | RecycleBinFlags.NoSound);

        if (result != 0)
        {
            return new CleanerExecutionResult(
                Id,
                DisplayName,
                0,
                0,
                1,
                $"Windows API error 0x{unchecked((uint)result):X8}.");
        }

        return new CleanerExecutionResult(
            Id,
            DisplayName,
            summary.ReclaimableBytes,
            summary.ItemCount,
            0,
            "Recycle Bin emptied.");
    }

    [Flags]
    private enum RecycleBinFlags : uint
    {
        NoConfirmation = 0x00000001,
        NoProgressUi = 0x00000002,
        NoSound = 0x00000004
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHQUERYRBINFO
    {
        public uint cbSize;
        public long i64Size;
        public long i64NumItems;
    }

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, RecycleBinFlags dwFlags);

    [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
    private static extern int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);
}
