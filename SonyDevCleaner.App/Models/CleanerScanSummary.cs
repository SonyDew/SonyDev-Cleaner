namespace SonyDevCleaner.App.Models;

public sealed record CleanerScanSummary(
    string Id,
    string DisplayName,
    string Description,
    long ReclaimableBytes,
    int ItemCount,
    string Status,
    bool CanClean,
    bool RequiresElevation);
