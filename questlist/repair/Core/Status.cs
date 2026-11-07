/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace QuestListRepair.Core;

enum Problem
{
    None,
    NoDiscord,
    NotInstalled,
    EquicordOut,
    Elsewhere,
    BuildMissing,
    OfficialBuild,
    PluginOff,
}

sealed record Snapshot(
    DiscordInstall? Discord,
    bool DiscordRunning,
    Injection Injection,
    AsarInfo Build,
    bool? QuestList,
    bool? Questify,
    Problem Problem)
{
    public static Snapshot Take()
    {
        var discord = Core.Discord.Find();
        var injection = discord == null ? new Injection(InjectionState.None, null) : Injector.Inspect(discord.ResourcesDir);
        var build = AsarInfo.Read(Paths.EquicordAsar);
        var questList = EquicordSettings.IsEnabled("QuestList");
        var questify = EquicordSettings.IsEnabled("Questify");

        var problem =
            discord == null ? Problem.NoDiscord
            : injection.State == InjectionState.Elsewhere ? Problem.Elsewhere
            : !build.Exists && injection.State != InjectionState.Equicord ? Problem.NotInstalled
            : injection.State != InjectionState.Equicord ? Problem.EquicordOut
            : !build.Exists ? Problem.BuildMissing
            : !build.IsQuestList ? Problem.OfficialBuild
            : questList != true ? Problem.PluginOff
            : Problem.None;

        return new Snapshot(discord, discord != null && Core.Discord.IsRunning(discord), injection, build, questList, questify, problem);
    }

    public string ToJson() => new JavaScriptSerializer().Serialize(new Dictionary<string, object?>
    {
        ["problem"] = Problem.ToString(),
        ["discord"] = Discord?.AppFolderName,
        ["discordRunning"] = DiscordRunning,
        ["injection"] = Injection.State.ToString(),
        ["injectionTarget"] = Injection.Target,
        ["buildExists"] = Build.Exists,
        ["buildHash"] = Build.Hash,
        ["buildIsQuestList"] = Build.IsQuestList,
        ["questList"] = QuestList,
        ["questify"] = Questify,
    });
}
