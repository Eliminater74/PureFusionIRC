; Inno Setup 6 script for PureFusionIRC.
; Compile with packaging/build-installer.ps1 (local or GitHub Actions).

#define MyAppName "PureFusionIRC"
#define MyAppPublisher "PureFusion"
#define MyAppURL "https://github.com/Eliminater74/PureFusionIRC"
#define MyAppExeName "PureFusionIRC.exe"

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0-B1"
#endif
#ifndef MyAppNumericVersion
  #define MyAppNumericVersion "1.0.0.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\artifacts\publish\win-x64"
#endif
#ifndef OutputDir
  #define OutputDir "..\artifacts\installer"
#endif

[Setup]
AppId={{8F3E21A6-9C74-4D1B-B2E8-7A15C9F04E6D}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}/issues
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
LicenseFile=..\LICENSE
OutputDir={#OutputDir}
OutputBaseFilename={#MyAppName}-{#MyAppVersion}-setup
SetupIconFile=..\src\PureFusionIRC.App\Assets\PureFusionIRC.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplayName={#MyAppName}
VersionInfoVersion={#MyAppNumericVersion}
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppNumericVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoCopyright=Copyright (c) 2026 PureFusionIRC contributors
CloseApplications=yes
RestartApplications=no
ChangesAssociations=no
; User IRC settings live in %AppData%\PureFusionIRC and are left in place on uninstall.

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "Windows IRC client"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
