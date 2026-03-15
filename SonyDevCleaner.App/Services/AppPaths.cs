using System.IO;

namespace SonyDevCleaner.App.Services;

public static class AppPaths
{
    private static readonly bool IsPortable =
        File.Exists(Path.Combine(AppContext.BaseDirectory, "portable.txt"));

    public static string DataDirectory => IsPortable
        ? AppContext.BaseDirectory
        : Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SonyDevCleaner");

    public static string StatsFile => Path.Combine(DataDirectory, "stats.json");

    public static string ScheduleFile => Path.Combine(DataDirectory, "schedule.json");

    public static string PortableHint => IsPortable ? " [Portable]" : string.Empty;
}
