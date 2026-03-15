param(
    [string]$Version = "1.0.0",
    [string]$Runtime = "win-x64",
    [switch]$SkipInstaller
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "SonyDevCleaner.App\SonyDevCleaner.App.csproj"
$releaseRoot = Join-Path $repoRoot "artifacts\release"
$publishRoot = Join-Path $releaseRoot "publish\$Runtime"
$portableRoot = Join-Path $releaseRoot "portable\SonyDev Cleaner"
$setupRoot = Join-Path $releaseRoot "setup\SonyDev Cleaner"
$portableZip = Join-Path $releaseRoot "SonyDevCleaner-Portable-$Version-$Runtime.zip"
$installerScript = Join-Path $repoRoot "installer\SonyDevCleaner.iss"
$innoCompiler = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"

Write-Host "Cleaning previous release artifacts..."
if (Test-Path $releaseRoot) {
    Remove-Item $releaseRoot -Recurse -Force
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
New-Item -ItemType Directory -Force -Path $portableRoot | Out-Null
New-Item -ItemType Directory -Force -Path $setupRoot | Out-Null

Write-Host "Publishing app..."
dotnet publish $projectPath `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -o $publishRoot

Write-Host "Preparing portable package..."
Copy-Item (Join-Path $publishRoot "*") -Destination $portableRoot -Recurse
Copy-Item (Join-Path $repoRoot "SonyDevCleaner.App\portable.txt") -Destination (Join-Path $portableRoot "portable.txt") -Force
Compress-Archive -Path (Join-Path $portableRoot "*") -DestinationPath $portableZip -Force

if (-not $SkipInstaller) {
    if (-not (Test-Path $innoCompiler)) {
        throw "Inno Setup compiler not found at '$innoCompiler'."
    }

    Write-Host "Preparing setup package..."
    Copy-Item (Join-Path $publishRoot "*") -Destination $setupRoot -Recurse

    Write-Host "Building installer..."
    & $innoCompiler `
        "/DMyAppVersion=$Version" `
        "/DMyAppSourceDir=$setupRoot" `
        "/DMyOutputDir=$releaseRoot" `
        $installerScript
}

Write-Host ""
Write-Host "Release artifacts ready:"
Get-ChildItem -Path $releaseRoot -File | Select-Object Name, Length
