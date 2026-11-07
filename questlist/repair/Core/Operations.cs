/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace QuestListRepair.Core;

enum StepState
{
    Pending,
    Running,
    Done,
    Skipped,
    Failed,
}

sealed class Step
{
    public Step(string title, Func<Step, Task> work)
    {
        Title = title;
        Work = work;
    }

    public string Title { get; }
    public Func<Step, Task> Work { get; }
    public StepState State { get; set; }
    public string? Detail { get; set; }
    public Operation? Owner { get; set; }
}

sealed class Operation
{
    public Operation(string title, string activity, List<Step> steps)
    {
        Title = title;
        ActivityTitle = activity;
        Steps = steps;
        foreach (var step in steps) step.Owner = this;
    }

    public string Title { get; }
    public string ActivityTitle { get; }
    public List<Step> Steps { get; }
    public double Progress { get; private set; }
    public string? Error { get; private set; }
    public event Action? Changed;

    public void Report(double stepFraction, Step step)
    {
        var index = Steps.IndexOf(step);
        Progress = (index + Math.Max(0, Math.Min(1, stepFraction))) / Steps.Count;
        Changed?.Invoke();
    }

    public async Task<bool> RunAsync()
    {
        Log.Write($"== {Title} ==");
        foreach (var step in Steps)
        {
            step.State = StepState.Running;
            Report(0, step);
            try
            {
                await step.Work(step).ConfigureAwait(false);
                if (step.State == StepState.Running) step.State = StepState.Done;
                Log.Write($"{step.Title}: {step.State}{(step.Detail == null ? "" : " (" + step.Detail + ")")}");
            }
            catch (Exception e)
            {
                step.State = StepState.Failed;
                Error = e.Message;
                Log.Write($"{step.Title}: failed: {e}");
                Activity.Add(ActivityTitle, Strings.T("activity_failed", e.Message));
                Changed?.Invoke();
                return false;
            }
            Report(1, step);
        }
        Activity.Add(ActivityTitle, Steps.FirstOrDefault(s => s.Detail != null && s.State == StepState.Done)?.Detail ?? Strings.T("activity_ok"));
        Log.Write($"== {Title}: done ==");
        return true;
    }
}

static class Operations
{
    static DiscordInstall RequireDiscord() => Discord.Find() ?? throw new IOException(Strings.T("err_no_discord"));

    static Step CloseStep(Action<bool> wasRunning) => new(Strings.T("step_close"), _ =>
    {
        var d = RequireDiscord();
        var running = Discord.IsRunning(d);
        wasRunning(running);
        if (running) Discord.Close(d);
        return Task.CompletedTask;
    });

    static Step BackupStep(string reason) => new(Strings.T("step_backup"), s =>
    {
        if (Backups.Create(reason) == null) s.State = StepState.Skipped;
        return Task.CompletedTask;
    });

    static Step DownloadStep(string repo, string? tag = null) => new(Strings.T("step_download"), async s =>
    {
        var release = await Releases.LatestAsync(repo, tag).ConfigureAwait(false);
        var installed = AsarInfo.Read(Paths.EquicordAsar);
        if (installed.Hash == release.Hash && installed.IsQuestList == (repo == Paths.Repo))
        {
            s.State = StepState.Skipped;
            s.Detail = release.Tag;
            return;
        }
        await Releases.DownloadAsync(release, Paths.EquicordAsar, new Progress<double>(f => s.Owner?.Report(f, s))).ConfigureAwait(false);
        s.Detail = release.Tag;
    });

    static Step InjectStep() => new(Strings.T("step_inject"), s =>
    {
        var d = RequireDiscord();
        if (Injector.Inspect(d.ResourcesDir).State == InjectionState.Equicord)
        {
            s.State = StepState.Skipped;
            return Task.CompletedTask;
        }
        Injector.Patch(d.ResourcesDir, Paths.EquicordAsar);
        return Task.CompletedTask;
    });

    static Step PluginsStep(string title, Dictionary<string, bool> changes) => new(title, _ =>
    {
        EquicordSettings.SetEnabled(changes);
        return Task.CompletedTask;
    });

    static Step StartStep(Func<bool> shouldStart) => new(Strings.T("step_start"), s =>
    {
        if (!shouldStart())
        {
            s.State = StepState.Skipped;
            return Task.CompletedTask;
        }
        Discord.Start(RequireDiscord());
        return Task.CompletedTask;
    });

    public static Operation Repair(bool install, bool? questify, bool start, string? tag = null)
    {
        var changes = new Dictionary<string, bool> { ["QuestList"] = true };
        if (questify != null) changes["Questify"] = questify.Value;
        return new Operation(
            Strings.T(install ? "op_install" : "op_repair"),
            Strings.T(install ? "activity_installed" : "activity_repaired"),
            new List<Step>
            {
                CloseStep(_ => { }),
                BackupStep(install ? "install" : "repair"),
                DownloadStep(Paths.Repo, tag),
                InjectStep(),
                PluginsStep(Strings.T("step_plugins"), changes),
                StartStep(() => start),
            });
    }

    public static Operation Official(bool start)
    {
        return new Operation(Strings.T("op_official"), Strings.T("activity_official"), new List<Step>
        {
            CloseStep(_ => { }),
            BackupStep("official"),
            DownloadStep(Paths.OfficialRepo),
            InjectStep(),
            StartStep(() => start),
        });
    }

    public static Operation Uninstall(bool removeEquicord, bool start)
    {
        if (!removeEquicord) return Official(start);
        return new Operation(Strings.T("op_uninstall"), Strings.T("activity_uninstalled"), new List<Step>
        {
            CloseStep(_ => { }),
            BackupStep("uninstall"),
            new(Strings.T("step_unpatch"), _ =>
            {
                Injector.Unpatch(RequireDiscord().ResourcesDir);
                return Task.CompletedTask;
            }),
            StartStep(() => start),
        });
    }

    public static Operation SetQuestify(bool enabled)
    {
        var wasRunning = false;
        return new Operation(Strings.T(enabled ? "op_questify_on" : "op_questify_off"), Strings.T(enabled ? "activity_questify_on" : "activity_questify_off"), new List<Step>
        {
            CloseStep(r => wasRunning = r),
            PluginsStep(Strings.T("step_questify"), new Dictionary<string, bool> { ["Questify"] = enabled }),
            StartStep(() => wasRunning),
        });
    }

    public static Operation Restore(Backup backup)
    {
        var wasRunning = false;
        return new Operation(Strings.T("op_restore"), Strings.T("activity_restored"), new List<Step>
        {
            CloseStep(r => wasRunning = r),
            BackupStep("restore"),
            new(Strings.T("step_restore"), s =>
            {
                Backups.Restore(backup);
                s.Detail = backup.When.ToString("dd/MM/yyyy HH:mm");
                return Task.CompletedTask;
            }),
            StartStep(() => wasRunning),
        });
    }
}
