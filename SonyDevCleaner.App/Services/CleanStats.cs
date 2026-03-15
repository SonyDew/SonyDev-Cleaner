namespace SonyDevCleaner.App.Services;

public sealed class CleanStats
{
    public long TotalBytesCleanedAllTime { get; set; }

    public long BytesCleanedThisMonth { get; set; }

    public int CleanRunsThisMonth { get; set; }

    public int CleanRunsAllTime { get; set; }

    public DateTime LastCleanDate { get; set; }

    public string LastCleanMonth { get; set; } = string.Empty;
}
