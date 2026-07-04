#define MyAppName "EHBYS"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Ege Hobi Bahceleri"
#define MyAppExeName "EHBYS.exe"

[Setup]
AppId={{7B32DE4A-0A34-44F0-9B1A-7E224982D9F5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
OutputDir=..\publish\Setup
OutputBaseFilename=Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "Masaustu kisayolu olustur"; GroupDescription: "Ek gorevler:"

[Files]
Source: "..\publish\EHBYS\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{#MyAppName} calistir"; Flags: nowait postinstall skipifsilent
