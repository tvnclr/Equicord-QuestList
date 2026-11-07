/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.IO;

namespace QuestListRepair.Core;

static class Paths
{
    public static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public static readonly string EquicordDir = Path.Combine(AppData, "Equicord");
    public static readonly string EquicordAsar = Path.Combine(EquicordDir, "equicord.asar");
    public static readonly string SettingsDir = Path.Combine(EquicordDir, "settings");
    public static readonly string SettingsJson = Path.Combine(SettingsDir, "settings.json");

    public static readonly string QuestListDir = Path.Combine(AppData, "QuestList");
    public static readonly string BackupDir = Path.Combine(QuestListDir, "backups");
    public static readonly string ActivityFile = Path.Combine(QuestListDir, "activity.log");
    public static readonly string LogDir = Path.Combine(LocalAppData, "QuestList", "logs");

    public const string Repo = "tvnclr/Equicord-QuestList";
    public const string OfficialRepo = "Equicord/Equicord";
    public const string RepoUrl = "https://github.com/" + Repo;
}
