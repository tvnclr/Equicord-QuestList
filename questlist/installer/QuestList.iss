; QuestList installer
; Copyright (c) 2026 tvnclr
; SPDX-License-Identifier: GPL-3.0-or-later

#ifndef AppVersion
  #define AppVersion "0.0.0"
#endif
#ifndef FileVersion
  #define FileVersion "0.0.0.0"
#endif
#ifndef BuildDir
  #define BuildDir "..\repair\bin\Release\net48"
#endif

[Setup]
AppId={{77143887-188D-4B44-B01B-AF1AEFB7A517}
AppName=QuestList
AppVersion={#AppVersion}
AppVerName=QuestList {#AppVersion}
AppPublisher=tvnclr
AppPublisherURL=https://github.com/tvnclr/Equicord-QuestList
AppSupportURL=https://github.com/tvnclr/Equicord-QuestList/issues
AppUpdatesURL=https://github.com/tvnclr/Equicord-QuestList/releases
DefaultDirName={localappdata}\Programs\QuestList
DisableDirPage=yes
DisableProgramGroupPage=yes
DisableReadyPage=no
PrivilegesRequired=lowest
MinVersion=10.0
OutputBaseFilename=QuestList-Setup
OutputDir=Output
SetupIconFile=..\branding\QuestList.ico
UninstallDisplayIcon={app}\QuestListRepair.exe
UninstallDisplayName=QuestList
LicenseFile=..\..\LICENSE
WizardStyle=modern dark includetitlebar hidebevels
WizardBackColor=#1E1D25
WizardImageFile=images\wizard-100.png,images\wizard-150.png,images\wizard-200.png
WizardSmallImageFile=images\small-100.png,images\small-150.png,images\small-200.png
ShowLanguageDialog=auto
CloseApplications=yes
Compression=lzma2/max
SolidCompression=yes
VersionInfoVersion={#FileVersion}
VersionInfoProductName=QuestList

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "ptbr"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[CustomMessages]
en.QuestifyCaption=Questify
en.QuestifyDescription=Optional plugin that completes Discord quests by itself.
en.QuestifyWarning=Questify automates quests, which goes against Discord's rules. Discord may suspend your quests for 14 days. You can turn it on or off later in QuestList Repair.
en.QuestifyCheck=Turn on Questify
en.ReadyQuestify=Questify:
en.ReadyOn=On
en.ReadyOff=Off
en.ReadyDiscord=Discord will close while QuestList is installed. Your Equicord settings are backed up first.
en.Patching=Installing QuestList in Discord...
en.InstallFailed=QuestList could not be installed in Discord.%n%n%1%n%nOpen QuestList Repair to see the log and try again.
en.OpenDiscord=Open Discord
en.OpenRepair=Open QuestList Repair
en.DesktopIcon=Create a desktop shortcut for QuestList Repair
ptbr.QuestifyCaption=Questify
ptbr.QuestifyDescription=Plugin opcional que completa as quests do Discord sozinho.
ptbr.QuestifyWarning=O Questify automatiza as quests, o que vai contra as regras do Discord. O Discord pode suspender suas quests por 14 dias. Você pode ligar ou desligar depois no QuestList Repair.
ptbr.QuestifyCheck=Ligar o Questify
ptbr.ReadyQuestify=Questify:
ptbr.ReadyOn=Ligado
ptbr.ReadyOff=Desligado
ptbr.ReadyDiscord=O Discord vai fechar enquanto o QuestList é instalado. Suas configurações do Equicord ganham um backup antes.
ptbr.Patching=Instalando o QuestList no Discord...
ptbr.InstallFailed=Não deu pra instalar o QuestList no Discord.%n%n%1%n%nAbra o QuestList Repair pra ver o log e tentar de novo.
ptbr.OpenDiscord=Abrir o Discord
ptbr.OpenRepair=Abrir o QuestList Repair
ptbr.DesktopIcon=Criar atalho do QuestList Repair na área de trabalho

[Tasks]
Name: "desktopicon"; Description: "{cm:DesktopIcon}"; Flags: unchecked

[Files]
Source: "{#BuildDir}\QuestListRepair.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\QuestListRepair.exe.config"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\QuestList Repair"; Filename: "{app}\QuestListRepair.exe"
Name: "{autodesktop}\QuestList Repair"; Filename: "{app}\QuestListRepair.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\QuestListRepair.exe"; Parameters: "--start-discord"; Description: "{cm:OpenDiscord}"; Flags: postinstall nowait skipifsilent; Check: Installed
Filename: "{app}\QuestListRepair.exe"; Description: "{cm:OpenRepair}"; Flags: postinstall nowait skipifsilent; Check: not Installed

[UninstallRun]
Filename: "{app}\QuestListRepair.exe"; Parameters: "--uninstall --skip-if-done"; RunOnceId: "RemoveQuestList"; Flags: runhidden waituntilterminated

[Code]
var
  QuestifyPage: TInputOptionWizardPage;
  InstallOk: Boolean;

function Installed: Boolean;
begin
  Result := InstallOk;
end;

procedure InitializeWizard;
begin
  QuestifyPage := CreateInputOptionPage(wpLicense, CustomMessage('QuestifyCaption'), CustomMessage('QuestifyDescription'), CustomMessage('QuestifyWarning'), False, False);
  QuestifyPage.Add(CustomMessage('QuestifyCheck'));
  QuestifyPage.Values[0] := GetPreviousData('Questify', '0') = '1';
end;

procedure RegisterPreviousData(PreviousDataKey: Integer);
begin
  if QuestifyPage.Values[0] then
    SetPreviousData(PreviousDataKey, 'Questify', '1')
  else
    SetPreviousData(PreviousDataKey, 'Questify', '0');
end;

function UpdateReadyMemo(Space, NewLine, MemoUserInfoInfo, MemoDirInfo, MemoTypeInfo, MemoComponentsInfo, MemoGroupInfo, MemoTasksInfo: String): String;
var
  State: String;
begin
  if QuestifyPage.Values[0] then State := CustomMessage('ReadyOn') else State := CustomMessage('ReadyOff');
  Result := CustomMessage('ReadyQuestify') + NewLine + Space + State;
  if MemoTasksInfo <> '' then Result := Result + NewLine + NewLine + MemoTasksInfo;
  Result := Result + NewLine + NewLine + CustomMessage('ReadyDiscord');
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Questify, ResultFile, Detail: String;
  Lines: TArrayOfString;
  Code: Integer;
begin
  if CurStep <> ssPostInstall then Exit;
  if QuestifyPage.Values[0] then Questify := 'on' else Questify := 'off';
  ResultFile := ExpandConstant('{tmp}\install-result.txt');
  WizardForm.StatusLabel.Caption := CustomMessage('Patching');
  WizardForm.ProgressGauge.Style := npbstMarquee;
  InstallOk := Exec(ExpandConstant('{app}\QuestListRepair.exe'), '--install --questify=' + Questify + ' --no-start --out "' + ResultFile + '"', '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0);
  WizardForm.ProgressGauge.Style := npbstNormal;
  if InstallOk then Exit;
  Detail := SysErrorMessage(Code);
  if LoadStringsFromFile(ResultFile, Lines) and (GetArrayLength(Lines) > 0) then Detail := Lines[0];
  SuppressibleMsgBox(FmtMessage(CustomMessage('InstallFailed'), [Detail]), mbError, MB_OK, IDOK);
end;
