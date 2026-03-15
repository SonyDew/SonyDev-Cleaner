# Publishing Guide

This document describes a simple release flow for SonyDev Cleaner.

## Prerequisites

- Windows machine
- .NET 8 SDK installed
- Git configured
- GitHub CLI authenticated with `gh auth login`

## Build

```powershell
dotnet restore SonyDevCleaner.sln
dotnet build SonyDevCleaner.sln -c Release
```

## Create a Local Release Build

```powershell
.\scripts\Build-Release.ps1 -Version 1.0.0
```

This produces:

- `artifacts/release/SonyDevCleaner-Portable-<version>-win-x64.zip`
- `artifacts/release/SonyDevCleaner-Setup-<version>-win-x64.exe`

If you only want the portable package:

```powershell
.\scripts\Build-Release.ps1 -Version 1.0.0 -SkipInstaller
```

## Portable Build

The portable package is built automatically by the release script. It places `portable.txt` next to the executable in the packaged output.

Portable mode stores app data next to the executable instead of `%LOCALAPPDATA%\SonyDevCleaner\`.

## Release Checklist

- update `CHANGELOG.md`
- verify `dotnet build SonyDevCleaner.sln`
- smoke-test scan, cleanup, tray behavior, and scheduled scan toggle
- confirm screenshots in `docs/screenshots/` still match the current UI
- tag and publish a GitHub release

## GitHub Release Example

```powershell
git tag v1.0.1
git push origin main --tags
gh release create v1.0.1 --generate-notes
```

## Suggested Assets

- source zip from GitHub
- `SonyDevCleaner-Portable-<version>-win-x64.zip`
- `SonyDevCleaner-Setup-<version>-win-x64.exe`
- release notes summarizing visible changes and validation
