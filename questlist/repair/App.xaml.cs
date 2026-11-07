/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using QuestListRepair.Core;

namespace QuestListRepair;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var args = e.Args.Select(a => a.ToLowerInvariant()).ToArray();
        if (args.Length == 0)
        {
            new MainWindow().Show();
            return;
        }

        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        int code;
        try
        {
            code = await RunCliAsync(e.Args);
        }
        catch (Exception ex)
        {
            Log.Write("CLI crashed: " + ex);
            code = 1;
        }
        Shutdown(code);
    }

    static string? Value(string[] args, string name)
    {
        var i = Array.FindIndex(args, a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));
        if (i >= 0 && i + 1 < args.Length) return args[i + 1];
        var inline = args.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase));
        return inline?.Substring(name.Length + 1);
    }

    static bool Has(string[] args, string name) => args.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase));

    static async Task<int> RunCliAsync(string[] args)
    {
        Log.Write("CLI: " + string.Join(" ", args));
        var output = Value(args, "--out");
        var start = !Has(args, "--no-start");

        if (Has(args, "--check-strings"))
        {
            var problems = Strings.Check();
            if (output != null) File.WriteAllLines(output, problems.DefaultIfEmpty("ok"), Encoding.UTF8);
            return problems.Count == 0 ? 0 : 1;
        }

        if (Has(args, "--status"))
        {
            var json = Snapshot.Take().ToJson();
            if (output != null) File.WriteAllText(output, json, Encoding.UTF8);
            Log.Write("Status: " + json);
            return 0;
        }

        Operation? operation = null;
        if (Has(args, "--install") || Has(args, "--repair"))
        {
            var questify = Value(args, "--questify") switch
            {
                "on" or "1" or "true" => true,
                "off" or "0" or "false" => (bool?)false,
                _ => null,
            };
            operation = Operations.Repair(Has(args, "--install"), questify, start);
        }
        else if (Has(args, "--official"))
            operation = Operations.Official(start);
        else if (Has(args, "--uninstall"))
        {
            var marker = Path.Combine(Paths.QuestListDir, "uninstall-done");
            if (Has(args, "--skip-if-done") && File.Exists(marker))
            {
                File.Delete(marker);
                Log.Write("Uninstall already done from the app");
                return 0;
            }
            operation = Operations.Uninstall(Has(args, "--remove-equicord"), start);
        }

        if (operation == null)
        {
            Log.Write("Unknown arguments");
            return 2;
        }

        var ok = await operation.RunAsync();
        if (output != null) File.WriteAllText(output, (ok ? "ok" : "failed: " + operation.Error) + Environment.NewLine + Snapshot.Take().ToJson(), Encoding.UTF8);
        return ok ? 0 : 1;
    }
}
