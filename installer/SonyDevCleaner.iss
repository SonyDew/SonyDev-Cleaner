#define MyAppName "SonyDev Cleaner"
#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif
#ifndef MyAppPublisher
  #define MyAppPublisher "SonyDew"
#endif
#ifndef MyAppExeName
  #define MyAppExeName "SonyDevCleaner.App.exe"
#endif
#ifndef MyAppSourceDir
  #define MyAppSourceDir "..\artifacts\release\setup\SonyDev Cleaner"
#endif
#ifndef MyOutputDir
  #define MyOutputDir "..\artifacts\release"
#endif

[Setup]
AppId={{C3B8B4B5-0F0F-4CF3-B8A3-B7A42B5DF00C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\SonyDev Cleaner
DefaultGroupName={#MyAppName}
LicenseFile=..\LICENSE
UninstallDisplayIcon={app}\{#MyAppExeName}
OutputDir={#MyOutputDir}
OutputBaseFilename=SonyDevCleaner-Setup-{#MyAppVersion}-win-x64
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
DisableProgramGroupPage=yes
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"

[Files]
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
