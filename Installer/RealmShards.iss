#define MyAppName "RealmShards"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "RealmShards"
#define MyAppExeName "RealmShards.exe"

; Build after the Unity Windows player exists in ..\Builds\Windows:
;   ISCC.exe Installer\RealmShards.iss
; Installer output stays in Builds\Installer, which is intentionally ignored by Git.

[Setup]
AppId={{A2D5C2AA-5BF4-4FD9-A71A-A6B1FC2BFA8C}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
OutputDir=..\Builds\Installer
OutputBaseFilename=RealmShards-Setup-{#MyAppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; The Unity player must remain intact: its executable, data directory, DLLs, and crash handler
; are all copied from the Windows build output.
Source: "..\Builds\Windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch {#MyAppName}"; Flags: nowait postinstall skipifsilent
