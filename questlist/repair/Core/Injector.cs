/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 *
 * The loader format (a tiny app.asar that requires the Equicord build, with the
 * original renamed to _app.asar) follows Equilotl, the Equicord installer:
 * https://github.com/Equicord/Equilotl (GPL-3.0).
 */

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace QuestListRepair.Core;

enum InjectionState
{
    None,
    Equicord,
    Elsewhere,
    Broken,
}

sealed record Injection(InjectionState State, string? Target);

static class Injector
{
    const string PackageJson = "{\n\t\"name\": \"discord\",\n\t\"main\": \"index.js\"\n}";
    const long MaxLoaderSize = 128 * 1024;
    static readonly Regex RequireRx = new("require\\((\"(?:[^\"\\\\]|\\\\.)*\")\\)", RegexOptions.Compiled);
    static readonly JavaScriptSerializer Json = new();

    public static Injection Inspect(string resources)
    {
        var app = Path.Combine(resources, "app.asar");
        var original = Path.Combine(resources, "_app.asar");

        if (Directory.Exists(app))
        {
            var index = Path.Combine(app, "index.js");
            var target = File.Exists(index) ? ParseTarget(File.ReadAllText(index, Encoding.UTF8)) : null;
            return target == null ? new(InjectionState.Broken, null) : Classify(target);
        }
        if (!File.Exists(app)) return new(InjectionState.Broken, null);
        if (!File.Exists(original) || new FileInfo(app).Length > MaxLoaderSize) return new(InjectionState.None, null);

        var loaderTarget = ParseTarget(File.ReadAllText(app, Encoding.UTF8));
        return loaderTarget == null ? new(InjectionState.None, null) : Classify(loaderTarget);
    }

    static Injection Classify(string target)
    {
        var same = string.Equals(Path.GetFullPath(target).TrimEnd('\\'), Path.GetFullPath(Paths.EquicordAsar), StringComparison.OrdinalIgnoreCase);
        return new(same ? InjectionState.Equicord : InjectionState.Elsewhere, target);
    }

    static string? ParseTarget(string text)
    {
        var m = RequireRx.Match(text);
        if (!m.Success) return null;
        try { return Json.Deserialize<string>(m.Groups[1].Value); }
        catch (ArgumentException) { return null; }
    }

    public static void Patch(string resources, string target)
    {
        var app = Path.Combine(resources, "app.asar");
        var original = Path.Combine(resources, "_app.asar");

        var current = Inspect(resources);
        if (current.State is InjectionState.Equicord or InjectionState.Elsewhere || Directory.Exists(app))
            Unpatch(resources);
        else if (File.Exists(original))
            File.Delete(original);

        File.Move(app, original);
        try
        {
            WriteLoader(app, target);
        }
        catch
        {
            if (File.Exists(app)) File.Delete(app);
            File.Move(original, app);
            throw;
        }
        Log.Write($"Loader written in {resources} -> {target}");
    }

    public static void Unpatch(string resources)
    {
        var app = Path.Combine(resources, "app.asar");
        var original = Path.Combine(resources, "_app.asar");
        var temp = Path.Combine(resources, "app.asar.tmp");
        if (!File.Exists(original)) return;

        if (Directory.Exists(temp)) Directory.Delete(temp, true);
        if (File.Exists(temp)) File.Delete(temp);

        if (Directory.Exists(app)) Directory.Move(app, temp);
        else if (File.Exists(app)) File.Move(app, temp);

        File.Move(original, app);

        if (Directory.Exists(temp)) Directory.Delete(temp, true);
        else if (File.Exists(temp)) File.Delete(temp);
        Log.Write($"Original app.asar restored in {resources}");
    }

    public static void WriteLoader(string outFile, string target)
    {
        var index = "require(" + Json.Serialize(target) + ")";
        var indexBytes = Encoding.UTF8.GetByteCount(index);
        var packageBytes = Encoding.UTF8.GetByteCount(PackageJson);

        var header = "{\"files\":{\"index.js\":{\"size\":" + indexBytes + ",\"offset\":\"0\"},\"package.json\":{\"size\":" + packageBytes + ",\"offset\":\"" + indexBytes + "\"}}}";
        var headerSize = (uint)Encoding.UTF8.GetByteCount(header);
        const uint dataSize = 4;
        var aligned = (headerSize + dataSize - 1) & ~(dataSize - 1);
        header += new string('0', (int)(aligned - headerSize));

        using var file = File.Create(outFile);
        using var writer = new BinaryWriter(file, Encoding.UTF8);
        writer.Write((int)dataSize);
        writer.Write((int)(aligned + 8));
        writer.Write((int)(aligned + dataSize));
        writer.Write((int)headerSize);
        writer.Write(Encoding.UTF8.GetBytes(header));
        writer.Write(Encoding.UTF8.GetBytes(index));
        writer.Write(Encoding.UTF8.GetBytes(PackageJson));
    }
}
