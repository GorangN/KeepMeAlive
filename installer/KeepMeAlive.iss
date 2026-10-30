#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#ifndef PublishDirectory
  #error PublishDirectory must identify the self-contained publish output.
#endif
#ifndef PackageDirectory
  #define PackageDirectory "..\artifacts\packages"
#endif

[Setup]
AppId={{F474B826-9E63-46B6-AB2F-2A419B8EDD73}
AppName=KeepMeAlive
AppVersion={#AppVersion}
AppPublisher=KeepMeAlive
AppPublisherURL=https://github.com/GorangN/KeepMeAlive
AppSupportURL=https://github.com/GorangN/KeepMeAlive/issues
DefaultDirName={localappdata}\Programs\KeepMeAlive
DefaultGroupName=KeepMeAlive
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.19045
OutputDir={#PackageDirectory}
OutputBaseFilename=KeepMeAlive-Setup
SetupIconFile=..\KeepMeAlive\Resources\Icons\tray_inactive.ico
WizardImageFile=..\KeepMeAlive\Resources\Icons\wizard_large.bmp
WizardSmallImageFile=..\KeepMeAlive\Resources\Icons\wizard_small.bmp
UninstallDisplayIcon={app}\KeepMeAlive.exe
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
LicenseFile=..\LICENSE
VersionInfoVersion={#AppVersion}

[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Excludes: "KeepMeAlive.portable,Data\*,Secrets\*,*.pdb,*.xml,*.bootstrap.json"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\KeepMeAlive"; Filename: "{app}\KeepMeAlive.exe"
Name: "{group}\Uninstall KeepMeAlive"; Filename: "{uninstallexe}"; IconFilename: "{app}\KeepMeAlive.exe"

[Run]
Filename: "{app}\KeepMeAlive.exe"; Description: "Open KeepMeAlive"; Flags: nowait postinstall skipifsilent

[Code]
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  StartupCommand: String;
  InstalledCommand: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    InstalledCommand := '"' + ExpandConstant('{app}\KeepMeAlive.exe') + '"';
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'KeepMeAlive', StartupCommand) then
    begin
      if (CompareText(StartupCommand, InstalledCommand) = 0) or
         (CompareText(StartupCommand, InstalledCommand + ' --silent') = 0) then
      begin
        RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'KeepMeAlive');
      end;
    end;
  end;
end;
