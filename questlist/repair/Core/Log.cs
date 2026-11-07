/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace QuestListRepair.Core;

static class Log
{
    static readonly object Gate = new();

    public static event Action<string>? Line;

    public static string TodayFile => Path.Combine(Paths.LogDir, $"repair-{DateTime.Now:yyyyMMdd}.log");

    public static void Write(string message)
    {
        var line = $"{DateTime.Now:HH:mm:ss}  {message}";
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(Paths.LogDir);
                File.AppendAllText(TodayFile, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        Line?.Invoke(line);
    }

    public static string ReadToday() => File.Exists(TodayFile) ? File.ReadAllText(TodayFile, Encoding.UTF8) : "";
}

sealed record ActivityEntry(DateTime When, string Title, string Detail);

static class Activity
{
    public static void Add(string title, string detail)
    {
        try
        {
            Directory.CreateDirectory(Paths.QuestListDir);
            var line = $"{DateTime.Now.ToString("s", CultureInfo.InvariantCulture)}|{title.Replace('|', '/')}|{detail.Replace('|', '/')}";
            File.AppendAllText(Paths.ActivityFile, line + Environment.NewLine, Encoding.UTF8);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    public static List<ActivityEntry> Recent(int count)
    {
        if (!File.Exists(Paths.ActivityFile)) return new();
        return File.ReadAllLines(Paths.ActivityFile, Encoding.UTF8)
            .Select(l => l.Split('|'))
            .Where(p => p.Length == 3 && DateTime.TryParse(p[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .Select(p => new ActivityEntry(DateTime.Parse(p[0], CultureInfo.InvariantCulture), p[1], p[2]))
            .Reverse()
            .Take(count)
            .ToList();
    }
}
