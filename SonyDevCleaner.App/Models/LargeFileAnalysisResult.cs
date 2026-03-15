namespace SonyDevCleaner.App.Models;

public sealed record LargeFileAnalysisResult(
    IReadOnlyList<LargeFileRecord> LargestFiles,
    int ScannedFileCount,
    long ScannedBytes,
    int SkippedDirectoryCount);
