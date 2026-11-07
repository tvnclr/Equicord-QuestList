/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace QuestListRepair.Core;

sealed record Backup(string File, DateTime When, string Reason, int Plugins);

static class Backups
{
    const int Keep = 10;
    const string Stamp = "yyyyMMdd-HHmmss";

    public static string? Create(string reason)
    {
        if (!Directory.Exists(Paths.SettingsDir)) return null;
        Directory.CreateDirectory(Paths.BackupDir);
        var file = Path.Combine(Paths.BackupDir, $"settings-{DateTime.Now.ToString(Stamp, CultureInfo.InvariantCulture)}-{reason}.zip");
        if (File.Exists(file)) File.Delete(file);
        ZipFile.CreateFromDirectory(Paths.SettingsDir, file, CompressionLevel.Optimal, false);
        foreach (var old in List().Skip(Keep)) File.Delete(old.File);
        Log.Write($"Backup created: {file}");
        return file;
    }

    public static List<Backup> List()
    {
        if (!Directory.Exists(Paths.BackupDir)) return new();
        var result = new List<Backup>();
        foreach (var file in Directory.GetFiles(Paths.BackupDir, "settings-*.zip"))
        {
            var parts = Path.GetFileNameWithoutExtension(file).Split('-');
            if (parts.Length < 4 || !DateTime.TryParseExact(parts[1] + "-" + parts[2], Stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var when))
                continue;
            result.Add(new Backup(file, when, parts[3], CountPlugins(file)));
        }
        return result.OrderByDescending(b => b.When).ToList();
    }

    static int CountPlugins(string zip)
    {
        try
        {
            using var archive = ZipFile.OpenRead(zip);
            var entry = archive.GetEntry("settings.json");
            if (entry == null) return 0;
            var temp = Path.GetTempFileName();
            entry.ExtractToFile(temp, true);
            var count = EquicordSettings.EnabledCount(temp);
            File.Delete(temp);
            return count;
        }
        catch (InvalidDataException) { return 0; }
        catch (IOException) { return 0; }
    }

    public static void Restore(Backup backup)
    {
        using var archive = ZipFile.OpenRead(backup.File);
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;
            var target = Path.GetFullPath(Path.Combine(Paths.SettingsDir, entry.FullName));
            if (!target.StartsWith(Path.GetFullPath(Paths.SettingsDir), StringComparison.OrdinalIgnoreCase)) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
        }
        Log.Write($"Backup restored: {backup.File}");
    }
}
