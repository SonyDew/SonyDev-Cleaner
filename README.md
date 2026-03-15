# SonyDev Cleaner

![License](https://img.shields.io/badge/license-GPL--3.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11-0078D4)

Modern Windows cleaner built with WPF on .NET 8. The project focuses on safe cleanup, clear previews, modular tools, and a polished desktop UI instead of aggressive "optimizer" behavior.

## Highlights

- Safe cleanup workflow with preview-first scanning
- Separate modules for Cleanup, Large Files, Activity, Startup Apps, and Disk Usage
- Tray support, background scan mode, scheduled weekly scan, and portable mode
- WPF glass-style interface with custom navigation and dashboard pages
- Windows-native cleanup targets such as temp files, caches, crash data, and app/browser caches

## Feature Tour

### Safe Cleanup

- Scans cleanup targets and shows reclaimable size before deleting anything
- Supports filtering and grouped categories
- Handles restricted folders gracefully and surfaces elevation requirements
- Exports text reports after a scan

### Home Dashboard

- Live reclaimable/selected/ready metrics
- Cleanup statistics persisted across runs
- Weekly scheduled scan toggle
- Quick utilities for DNS cache flush and Recycle Bin cleanup

### Large Files

- Manual analyzer for large files in any selected folder
- Keeps destructive actions separate from analysis

### Startup Apps

- Reads startup entries from Registry and Startup folders
- Can enable/disable supported entries without deleting them

### Disk Usage

- Lightweight per-drive folder size overview
- Top folders list plus visual bar breakdown

## Safety Philosophy

SonyDev Cleaner intentionally avoids:

- Registry cleaning
- Hidden "FPS boost" style tweaks
- Silent destructive actions without preview
- Bundled third-party software

The project prefers visibility, reversible actions where possible, and clear status feedback.

## Requirements

- Windows 10 or Windows 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Administrator rights only for features that explicitly require elevation

## Getting Started

```powershell
git clone <your-repo-url>
cd SonyDevCleaner
dotnet restore
dotnet build SonyDevCleaner.sln
dotnet run --project .\SonyDevCleaner.App\SonyDevCleaner.App.csproj
```

## Project Structure

```text
SonyDevCleaner.sln
SonyDevCleaner.App/
  Helpers/
  Models/
  Pages/
  Services/
  App.xaml
  MainWindow.xaml
```

## Portable Mode

The app supports portable mode.

- If a file named `portable.txt` exists next to the built `.exe`, app data is stored next to the executable.
- Otherwise data is stored in `%LOCALAPPDATA%\SonyDevCleaner\`.

Source builds include [`portable.txt`](./SonyDevCleaner.App/portable.txt) as a project file, but it is not copied to the output folder automatically.

## Scheduled Weekly Scan

The app can register a Windows Task Scheduler entry for a weekly background scan.

- Default schedule: Sunday at `10:00`
- The background scan runs silently and reports its result through a tray notification

## Build Notes

- Main desktop app: WPF
- Background tray balloon support: Windows Forms interop
- Target framework: `net8.0-windows`

## Contributing

Contributions are welcome. Start with [CONTRIBUTING.md](./CONTRIBUTING.md).

If you want to report a vulnerability, use [SECURITY.md](./SECURITY.md) instead of opening a public issue first.

## Screens and Assets

This repository is source-first. If you publish screenshots for GitHub later, add them under `docs/` and link them here.

## License

This project is licensed under the [GNU General Public License v3.0](./LICENSE).
