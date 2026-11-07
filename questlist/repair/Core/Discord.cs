/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace QuestListRepair.Core;

sealed record DiscordInstall(string Name, string Root, string ProcessName, string AppDir, Version Version)
{
    public string ResourcesDir => Path.Combine(AppDir, "resources");
    public string AppFolderName => Path.GetFileName(AppDir);
}

static class Discord
{
    static readonly (string Folder, string Process, string Name)[] Branches =
    {
        ("Discord", "Discord", "Discord"),
        ("DiscordPTB", "DiscordPTB", "Discord PTB"),
        ("DiscordCanary", "DiscordCanary", "Discord Canary"),
    };

    public static DiscordInstall? Find()
    {
        foreach (var (folder, process, name) in Branches)
        {
            var root = Path.Combine(Paths.LocalAppData, folder);
            if (!Directory.Exists(root)) continue;

            var latest = Directory.GetDirectories(root, "app-*")
                .Select(d => (Dir: d, Ok: Version.TryParse(Path.GetFileName(d).Substring(4), out var v), Ver: v))
                .Where(x => x.Ok && x.Ver != null && Directory.Exists(Path.Combine(x.Dir, "resources")))
                .OrderBy(x => x.Ver)
                .LastOrDefault();
            if (latest.Dir == null || latest.Ver == null) continue;

            return new DiscordInstall(name, root, process, latest.Dir, latest.Ver);
        }
        return null;
    }

    public static bool IsRunning(DiscordInstall d) => Process.GetProcessesByName(d.ProcessName).Length > 0;

    public static int Close(DiscordInstall d)
    {
        var processes = Process.GetProcessesByName(d.ProcessName);
        foreach (var p in processes)
        {
            try { p.Kill(); }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }
        }
        var deadline = DateTime.Now.AddSeconds(15);
        while (DateTime.Now < deadline && Process.GetProcessesByName(d.ProcessName).Length > 0)
            Thread.Sleep(250);
        if (Process.GetProcessesByName(d.ProcessName).Length > 0)
            throw new IOException(Strings.T("err_discord_close"));
        Thread.Sleep(500);
        return processes.Length;
    }

    public static void Start(DiscordInstall d)
    {
        var update = Path.Combine(d.Root, "Update.exe");
        var psi = File.Exists(update)
            ? new ProcessStartInfo(update, $"--processStart {d.ProcessName}.exe")
            : new ProcessStartInfo(Path.Combine(d.AppDir, d.ProcessName + ".exe"));
        psi.UseShellExecute = false;
        psi.WorkingDirectory = d.Root;
        Process.Start(psi);
    }
}
