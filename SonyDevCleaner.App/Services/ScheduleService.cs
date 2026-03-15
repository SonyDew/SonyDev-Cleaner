using System.Diagnostics;

namespace SonyDevCleaner.App.Services;

public sealed class ScheduleService
{
    private const string TaskName = "SonyDevCleaner_WeeklyScan";

    public bool IsScheduled()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("schtasks", $"/Query /TN \"{TaskName}\"")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool Enable()
    {
        try
        {
            var executablePath = Environment.ProcessPath
                                 ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            var taskCommand = executablePath.Contains(' ')
                ? $"\\\"{executablePath}\\\" --background-scan"
                : $"{executablePath} --background-scan";
            var arguments = $"/Create /TN \"{TaskName}\" /TR \"{taskCommand}\" /SC WEEKLY /D SUN /ST 10:00 /F";

            using var process = Process.Start(new ProcessStartInfo("schtasks", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public bool Disable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("schtasks", $"/Delete /TN \"{TaskName}\" /F")
            {
                CreateNoWindow = true,
                UseShellExecute = false
            });
            process?.WaitForExit();
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
