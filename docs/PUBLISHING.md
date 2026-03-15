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
dotnet publish .\SonyDevCleaner.App\SonyDevCleaner.App.csproj `
  -c Release `
  -r win-x64 `
  --self-contained false `
  -o .\artifacts\publish\win-x64
```

## Portable Build

To publish a portable build, place `portable.txt` next to the final executable in the published output.

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
- optional `win-x64` published zip
- release notes summarizing visible changes and validation
