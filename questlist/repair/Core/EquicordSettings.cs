/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace QuestListRepair.Core;

static class EquicordSettings
{
    static JavaScriptSerializer Json => new() { MaxJsonLength = int.MaxValue, RecursionLimit = 256 };

    static Dictionary<string, object> Load(string file)
    {
        if (!File.Exists(file)) return new();
        var text = File.ReadAllText(file, Encoding.UTF8);
        return string.IsNullOrWhiteSpace(text) ? new() : Json.Deserialize<Dictionary<string, object>>(text) ?? new();
    }

    static Dictionary<string, object>? Plugins(Dictionary<string, object> settings) =>
        settings.TryGetValue("plugins", out var p) ? p as Dictionary<string, object> : null;

    public static bool? IsEnabled(string plugin, string? file = null)
    {
        var plugins = Plugins(Load(file ?? Paths.SettingsJson));
        if (plugins == null || !plugins.TryGetValue(plugin, out var raw) || raw is not Dictionary<string, object> entry) return null;
        return entry.TryGetValue("enabled", out var e) && e is bool b && b;
    }

    public static int EnabledCount(string file)
    {
        var plugins = Plugins(Load(file));
        return plugins?.Values.OfType<Dictionary<string, object>>().Count(p => p.TryGetValue("enabled", out var e) && e is bool b && b) ?? 0;
    }

    public static void SetEnabled(IDictionary<string, bool> changes)
    {
        var settings = Load(Paths.SettingsJson);
        if (Plugins(settings) is not { } plugins)
        {
            plugins = new Dictionary<string, object>();
            settings["plugins"] = plugins;
        }
        foreach (var change in changes)
        {
            if (!plugins.TryGetValue(change.Key, out var raw) || raw is not Dictionary<string, object> entry)
            {
                entry = new Dictionary<string, object>();
                plugins[change.Key] = entry;
            }
            entry["enabled"] = change.Value;
        }

        Directory.CreateDirectory(Paths.SettingsDir);
        var temp = Paths.SettingsJson + ".tmp";
        File.WriteAllText(temp, Json.Serialize(settings), new UTF8Encoding(false));
        if (File.Exists(Paths.SettingsJson)) File.Replace(temp, Paths.SettingsJson, null);
        else File.Move(temp, Paths.SettingsJson);
        Log.Write("Plugins set: " + string.Join(", ", changes.Select(c => $"{c.Key}={c.Value}")));
    }
}
