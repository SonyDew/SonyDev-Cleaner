namespace SonyDevCleaner.App.Models;

public sealed record LargeFileRecord(
    string FullPath,
    long SizeBytes,
    DateTime LastWriteTimeUtc);
