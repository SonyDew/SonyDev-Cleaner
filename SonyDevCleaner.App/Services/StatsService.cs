using System.IO;
using System.Text.Json;

namespace SonyDevCleaner.App.Services;

public sealed class StatsService
{
    public CleanStats Load()
    {
        try
        {
            if (!File.Exists(AppPaths.StatsFile))
            {
                return Normalize(new CleanStats());
            }

            var json = File.ReadAllText(AppPaths.StatsFile);
            return Normalize(JsonSerializer.Deserialize<CleanStats>(json) ?? new CleanStats());
        }
        catch
        {
            return Normalize(new CleanStats());
        }
    }

    public void RecordClean(long bytesFreed)
    {
        var stats = Load();
        var now = DateTime.Now;
        var monthKey = now.ToString("yyyy-MM");

        if (stats.LastCleanMonth != monthKey)
        {
            stats.BytesCleanedThisMonth = 0;
            stats.CleanRunsThisMonth = 0;
            stats.LastCleanMonth = monthKey;
        }

        stats.TotalBytesCleanedAllTime += bytesFreed;
        stats.BytesCleanedThisMonth += bytesFreed;
        stats.CleanRunsThisMonth++;
        stats.CleanRunsAllTime++;
        stats.LastCleanDate = now;

        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(
            AppPaths.StatsFile,
            JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }));
    }

    private static CleanStats Normalize(CleanStats stats)
    {
        var monthKey = DateTime.Now.ToString("yyyy-MM");
        if (stats.LastCleanMonth != string.Empty && stats.LastCleanMonth != monthKey)
        {
            stats.BytesCleanedThisMonth = 0;
            stats.CleanRunsThisMonth = 0;
        }

        if (stats.LastCleanMonth == string.Empty)
        {
            stats.LastCleanMonth = monthKey;
        }

        return stats;
    }
}
