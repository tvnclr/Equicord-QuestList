/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

namespace QuestListRepair.Core;

static class Setup
{
    public static readonly string InstallDir = Path.Combine(Paths.LocalAppData, "Programs", "QuestList");
    public static readonly string InstalledExe = Path.Combine(InstallDir, "QuestListRepair.exe");
    static readonly string Shortcut = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "QuestList Repair.lnk");
    const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\QuestList";

    static string Self => Assembly.GetExecutingAssembly().Location;

    public static string Version =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0";

    public static string? ReleaseTag => Version.Contains("-dev.") ? "v" + Version : null;

    public static bool IsSetup => Path.GetFileName(Self).IndexOf("setup", StringComparison.OrdinalIgnoreCase) >= 0;

    public static void CopyFiles()
    {
        if (string.Equals(Path.GetFullPath(Self), InstalledExe, StringComparison.OrdinalIgnoreCase)) return;
        foreach (var other in Process.GetProcessesByName("QuestListRepair").Where(p => p.Id != Process.GetCurrentProcess().Id))
        {
            other.Kill();
            other.WaitForExit(5000);
        }
        Directory.CreateDirectory(InstallDir);
        File.Copy(Self, InstalledExe, true);

        var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell", true));
        dynamic link = ((dynamic)shell).CreateShortcut(Shortcut);
        link.TargetPath = InstalledExe;
        link.WorkingDirectory = InstallDir;
        link.Description = "QuestList Repair";
        link.Save();

        using var key = Registry.CurrentUser.CreateSubKey(UninstallKey);
        key.SetValue("DisplayName", "QuestList");
        key.SetValue("DisplayVersion", Version);
        key.SetValue("Publisher", "tvnclr");
        key.SetValue("DisplayIcon", InstalledExe);
        key.SetValue("InstallLocation", InstallDir);
        key.SetValue("URLInfoAbout", Paths.RepoUrl);
        key.SetValue("UninstallString", $"\"{InstalledExe}\" --uninstall");
        key.SetValue("QuietUninstallString", $"\"{InstalledExe}\" --uninstall --quiet");
        key.SetValue("EstimatedSize", (int)(new FileInfo(InstalledExe).Length / 1024), RegistryValueKind.DWord);
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        Log.Write($"Copied to {InstallDir}");
    }

    public static void RemoveFiles()
    {
        File.Delete(Shortcut);
        Registry.CurrentUser.DeleteSubKeyTree(UninstallKey, false);
        if (!Directory.Exists(InstallDir)) return;
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c timeout /t 3 /nobreak >nul & rmdir /s /q \"{InstallDir}\"")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
        });
        Log.Write($"Removed {InstallDir}");
    }
}
