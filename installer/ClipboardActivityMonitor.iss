#ifndef MySourceDir
  #error 必须通过 /DMySourceDir=... 指定已构建程序目录
#endif

#ifndef MyOutputDir
  #define MyOutputDir "."
#endif

#ifndef MyAppVersion
  #define MyAppVersion "1.0.0"
#endif

#define MyAppName "Clipboard Activity Monitor"
#define MyAppExeName "ClipboardActivityMonitor.exe"

[Setup]
AppId={{B79B97C4-E0E4-4DF2-90AD-7AE099CEB4D0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=creamtea47
AppPublisherURL=https://github.com/creamtea47/clipboard-activity-monitor
AppSupportURL=https://github.com/creamtea47/clipboard-activity-monitor/issues
AppUpdatesURL=https://github.com/creamtea47/clipboard-activity-monitor/releases
DefaultDirName={localappdata}\Programs\ClipboardActivityMonitor
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir={#MyOutputDir}
OutputBaseFilename=ClipboardActivityMonitor-v{#MyAppVersion}-win-x64
SetupIconFile=..\Assets\AppIcon.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#MyAppVersion}.0
VersionInfoCompany=creamtea47
VersionInfoDescription=Clipboard Activity Monitor Installer
VersionInfoProductName={#MyAppName}
VersionInfoProductVersion={#MyAppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; 安装完整程序目录，排除仅用于调试的符号和日志文件。
Source: "{#MySourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; Excludes: "*.pdb,*.log"

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加任务："; Flags: unchecked

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent
