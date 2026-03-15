namespace SonyDevCleaner.App.Models;

public sealed record CleanerExecutionResult(
    string Id,
    string DisplayName,
    long DeletedBytes,
    int DeletedItems,
    int FailedItems,
    string Message);
