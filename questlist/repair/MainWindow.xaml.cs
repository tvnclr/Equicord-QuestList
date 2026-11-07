/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using QuestListRepair.Core;

namespace QuestListRepair;

public partial class MainWindow : Window
{
    enum Pill { Ok, Warn, Neutral }

    Snapshot? snapshot;
    bool busy;
    Action? dialogOk;
    readonly Dictionary<RadioButton, (ContentControl Icon, string Data, FrameworkElement Page)> nav = new();

    static Brush Res(string key) => (Brush)Application.Current.Resources[key];

    public MainWindow()
    {
        InitializeComponent();

        nav[NavStatus] = (NavStatusIcon, Icons.Pulse, StatusPage);
        nav[NavBackups] = (NavBackupsIcon, Icons.Archive, BackupsPage);
        nav[NavLog] = (NavLogIcon, Icons.Doc, LogPage);
        nav[NavAbout] = (NavAboutIcon, Icons.Info, AboutPage);

        ApplyStrings();
        UpdateNavIcons();
        SizeToScreen();

        Loaded += (_, _) =>
        {
            PlaceOrbs();
            AnimateOrbs();
            _ = RefreshAsync();
        };
        SizeChanged += (_, _) =>
        {
            PlaceOrbs();
            ApplyLayout();
        };
        StateChanged += (_, _) => Frame.BorderThickness = WindowState == WindowState.Maximized ? new Thickness(7) : new Thickness(1);
        Activated += (_, _) =>
        {
            if (!busy && StatusPage.Visibility == Visibility.Visible) _ = RefreshAsync();
        };
    }

    void SizeToScreen()
    {
        var area = SystemParameters.WorkArea;
        Width = Math.Max(MinWidth, Math.Min(1600, Math.Round(area.Width * 0.667)));
        Height = Math.Max(MinHeight, Math.Min(area.Height * 0.92, Math.Round(Width * 0.625)));
    }

    void ApplyStrings()
    {
        Title = Strings.T("app_title");
        NavStatusText.Text = Strings.T("nav_status");
        NavBackupsText.Text = Strings.T("nav_backups");
        NavLogText.Text = Strings.T("nav_log");
        NavAboutText.Text = Strings.T("nav_about");

        CheckButton.Content = WithIcon(Icons.Refresh, Strings.T("btn_check"), Res("Text"));
        ProblemLogButton.Content = Strings.T("btn_view_log");
        ProblemIcon.Content = Icons.Make(Icons.Alert, Res("Warning"));
        SectionStatus.Text = Strings.T("section_status");
        SectionPlugins.Text = Strings.T("section_plugins");
        SectionActions.Text = Strings.T("section_actions");
        SectionActivity.Text = Strings.T("section_activity");
        SectionSteps.Text = Strings.T("section_steps");
        SectionLog.Text = Strings.T("section_log");

        QuestifyIcon.Content = Icons.Make(Icons.Zap, Res("Text"));
        QuestifyTitle.Text = Strings.T("questify_title");
        QuestifySub.Text = Strings.T("questify_sub");
        RepairRow.Content = Row(Icons.Wrench, Strings.T("act_repair"), Strings.T("act_repair_sub"), false);
        OfficialRow.Content = Row(Icons.Undo, Strings.T("act_official"), Strings.T("act_official_sub"), false);
        UninstallRow.Content = Row(Icons.Trash, Strings.T("act_uninstall"), Strings.T("act_uninstall_sub"), true);

        ProgressSub.Text = Strings.T("progress_sub");
        ProgressBack.Content = Strings.T("btn_back");

        BackupsTitle.Text = Strings.T("backups_title");
        BackupsSub.Text = Strings.T("backups_sub");
        BackupNowButton.Content = WithIcon(Icons.Plus, Strings.T("btn_backup_now"), Brushes.White);
        BackupsFoot.Text = Strings.T("backups_foot");
        BackupsPath.Text = @"%APPDATA%\QuestList\backups";

        LogTitle.Text = Strings.T("log_title");
        LogSub.Text = Strings.T("log_sub");
        OpenLogsButton.Content = Strings.T("btn_open_folder");

        var version = Setup.Version;
        AboutTitle.Text = Strings.T("about_title");
        AboutVersion.Text = Strings.T("about_version", version);
        AboutBody.Text = Strings.T("about_body");
        RepoLinkText.Text = Strings.T("about_repo");
        EquicordLinkText.Text = Strings.T("about_equicord");
        AboutLicense.Text = Strings.T("about_license");
        VersionText.Text = Strings.T("ver_questlist", version);

        DialogCancel.Content = Strings.T("dlg_cancel");
    }

    static StackPanel WithIcon(string icon, string text, Brush brush)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        panel.Children.Add(Icons.Make(icon, brush, 16, 2));
        panel.Children.Add(new TextBlock { Text = text, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center });
        return panel;
    }

    static DockPanel Row(string icon, string title, string sub, bool danger)
    {
        var color = danger ? Res("AccentFg") : Res("Text");
        var dock = new DockPanel();
        var badge = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(12), Background = Res("Elevated"), Child = Icons.Make(icon, color) };
        DockPanel.SetDock(badge, Dock.Left);
        dock.Children.Add(badge);
        var chevron = Icons.Make(Icons.Chevron, Res("Text2"), 16, 2);
        DockPanel.SetDock(chevron, Dock.Right);
        dock.Children.Add(chevron);
        var text = new StackPanel { Margin = new Thickness(12, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = title, Style = (Style)Application.Current.Resources["Item"], Foreground = color });
        text.Children.Add(new TextBlock { Text = sub, Style = (Style)Application.Current.Resources["Cap"] });
        dock.Children.Add(text);
        return dock;
    }

    void UpdateNavIcons()
    {
        foreach (var entry in nav)
            entry.Value.Icon.Content = Icons.Make(entry.Value.Data, entry.Key.IsChecked == true ? Res("AccentFg") : Res("Text2"));
    }

    void ApplyLayout()
    {
        if (Lower == null) return;
        var wide = ActualWidth >= 1400;
        Lower.ColumnDefinitions.Clear();
        Lower.RowDefinitions.Clear();
        var columns = wide ? 3 : 2;
        for (var i = 0; i < columns; i++)
        {
            if (i > 0) Lower.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(12) });
            Lower.ColumnDefinitions.Add(new ColumnDefinition());
        }
        Lower.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        Lower.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        Grid.SetColumn(PluginsColumn, 0);
        Grid.SetColumn(ActionsColumn, 2);
        if (wide)
        {
            Grid.SetRow(ActivityColumn, 0);
            Grid.SetColumn(ActivityColumn, 4);
            Grid.SetColumnSpan(ActivityColumn, 1);
            ActivityColumn.Margin = new Thickness(0);
        }
        else
        {
            Grid.SetRow(ActivityColumn, 1);
            Grid.SetColumn(ActivityColumn, 0);
            Grid.SetColumnSpan(ActivityColumn, 3);
            ActivityColumn.Margin = new Thickness(0, 24, 0, 0);
        }
    }

    void PlaceOrbs()
    {
        Canvas.SetLeft(Orb1, ActualWidth - 320);
        Canvas.SetTop(Orb1, -200);
        Canvas.SetLeft(Orb2, ActualWidth * 0.45);
        Canvas.SetTop(Orb2, ActualHeight - 180);
    }

    void AnimateOrbs() => Ui.AnimateOrbs(Orb1Move, Orb1Scale, Orb2Move, Orb2Scale);

    async Task RefreshAsync()
    {
        var snap = await Task.Run(Snapshot.Take);
        snapshot = snap;
        NoteChanges(snap);
        RenderStatus(snap);
        RenderActivity();
    }

    static void NoteChanges(Snapshot snap)
    {
        var file = Path.Combine(Paths.QuestListDir, "state.txt");
        var lines = File.Exists(file) ? File.ReadAllLines(file) : Array.Empty<string>();
        string? Old(string key) => lines.FirstOrDefault(l => l.StartsWith(key + "="))?.Substring(key.Length + 1);

        var hash = snap.Build.IsQuestList ? snap.Build.Hash : null;
        var app = snap.Discord?.AppFolderName;
        if (hash != null && Old("hash") is { Length: > 0 } oldHash && oldHash != hash)
            Activity.Add(Strings.T("activity_updated"), snap.Build.ShortHash);
        if (app != null && Old("discord") is { Length: > 0 } oldApp && oldApp != app)
            Activity.Add(Strings.T("activity_discord_updated"), app);

        Directory.CreateDirectory(Paths.QuestListDir);
        File.WriteAllLines(file, new[] { "hash=" + (hash ?? Old("hash") ?? ""), "discord=" + (app ?? Old("discord") ?? "") });
    }

    void RenderStatus(Snapshot s)
    {
        var ok = s.Problem == Problem.None;
        StatusTitle.Text = Strings.T(ok ? "status_ok" : "status_problem");
        StatusSub.Text = Strings.T(ok ? "status_ok_sub" : "status_problem_sub");
        EquicordText.Text = s.Build.Hash == null ? "" : Strings.T("ver_equicord", s.Build.ShortHash);

        ProblemCard.Visibility = ok ? Visibility.Collapsed : Visibility.Visible;
        FixButton.Visibility = ok || s.Problem == Problem.NoDiscord ? Visibility.Collapsed : Visibility.Visible;
        FixButton.Content = WithIcon(Icons.Wrench, Strings.T(s.Problem == Problem.NotInstalled ? "btn_install_now" : "btn_repair_now"), Brushes.White);
        if (!ok)
        {
            ProblemTitle.Text = Strings.T("problem_" + s.Problem);
            ProblemSub.Text = Strings.T("problem_" + s.Problem + "_sub", s.Injection.Target ?? "");
        }

        Tiles.Children.Clear();
        Tiles.Children.Add(s.Discord == null
            ? Tile(Icons.Monitor, Strings.T("tile_discord"), Strings.T("pill_missing"), Pill.Warn, Strings.T("sub_not_found"), false)
            : Tile(Icons.Monitor, Strings.T("tile_discord"), Strings.T("pill_ok"), Pill.Ok, s.DiscordRunning ? Strings.T("sub_running", s.Discord.AppFolderName) : s.Discord.AppFolderName, true));
        Tiles.Children.Add(s.Injection.State switch
        {
            InjectionState.Equicord => Tile(Icons.Layers, Strings.T("tile_equicord"), Strings.T("pill_active"), Pill.Ok, Strings.T("sub_equicord_in"), false),
            InjectionState.Elsewhere => Tile(Icons.Layers, Strings.T("tile_equicord"), Strings.T("pill_other"), Pill.Warn, Strings.T("sub_equicord_other"), false),
            _ => Tile(Icons.Layers, Strings.T("tile_equicord"), Strings.T("pill_out"), Pill.Warn, Strings.T("sub_equicord_out"), false),
        });
        Tiles.Children.Add(!s.Build.Exists
            ? Tile(Icons.Refresh, Strings.T("tile_update"), Strings.T("pill_missing"), Pill.Warn, Paths.Repo, true)
            : s.Build.IsQuestList
                ? Tile(Icons.Refresh, Strings.T("tile_update"), Strings.T(s.Injection.State == InjectionState.Equicord ? "pill_on" : "pill_stopped"), s.Injection.State == InjectionState.Equicord ? Pill.Ok : Pill.Neutral, Paths.Repo, true)
                : Tile(Icons.Refresh, Strings.T("tile_update"), Strings.T("pill_other"), Pill.Warn, Strings.T("sub_update_official"), false));
        var pluginOn = s.QuestList == true && s.Build.IsQuestList;
        Tiles.Children.Add(Tile(Icons.List, Strings.T("tile_questlist"), Strings.T(pluginOn ? "pill_active" : "pill_off"), pluginOn ? Pill.Ok : Pill.Neutral, Strings.T(pluginOn ? "sub_plugin_on" : "sub_plugin_off"), false));

        QuestifySwitch.IsChecked = s.Questify == true;
        QuestifySwitch.IsEnabled = s.Problem == Problem.None;
        OfficialRow.IsEnabled = s.Discord != null;
        UninstallRow.IsEnabled = s.Discord != null;
        RepairRow.IsEnabled = s.Discord != null;
        ApplyLayout();
    }

    static Border Tile(string icon, string title, string pill, Pill kind, string sub, bool mono)
    {
        var (fg, bg) = kind switch
        {
            Pill.Ok => (Res("Success"), Res("SuccessSoft")),
            Pill.Warn => (Res("Warning"), Res("WarningSoft")),
            _ => (Res("Text2"), Res("Elevated")),
        };
        var top = new DockPanel();
        var badge = new Border { Width = 32, Height = 32, CornerRadius = new CornerRadius(12), Background = Res("Elevated"), Child = Icons.Make(icon, kind == Pill.Warn ? Res("Warning") : Res("Text")) };
        DockPanel.SetDock(badge, Dock.Left);
        top.Children.Add(badge);
        top.Children.Add(new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10, 4, 10, 4),
            Background = bg,
            Child = new TextBlock { Text = pill, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = fg },
        });

        var body = new StackPanel();
        body.Children.Add(top);
        body.Children.Add(new TextBlock { Text = title, Style = (Style)Application.Current.Resources["Item"], Margin = new Thickness(0, 12, 0, 0) });
        body.Children.Add(new TextBlock { Text = sub, Style = (Style)Application.Current.Resources[mono ? "MonoText" : "Cap"], TextWrapping = TextWrapping.NoWrap, TextTrimming = TextTrimming.CharacterEllipsis, ToolTip = sub });

        return new Border { Style = (Style)Application.Current.Resources["CardBorder"], Padding = new Thickness(16), Margin = new Thickness(0, 0, 12, 0), Child = body };
    }

    void RenderActivity()
    {
        ActivityList.Children.Clear();
        var entries = Activity.Recent(4);
        if (entries.Count == 0)
        {
            ActivityList.Children.Add(new TextBlock { Text = Strings.T("activity_empty"), Style = (Style)Application.Current.Resources["Cap"], Margin = new Thickness(16, 14, 16, 14) });
            return;
        }
        for (var i = 0; i < entries.Count; i++)
        {
            if (i > 0) ActivityList.Children.Add(new Border { Height = 1, Background = Res("Line"), Margin = new Thickness(12, 0, 12, 0) });
            var text = new StackPanel { Margin = new Thickness(16, 10, 16, 10) };
            text.Children.Add(new TextBlock { Text = entries[i].Title, Style = (Style)Application.Current.Resources["Item"] });
            text.Children.Add(new TextBlock { Text = $"{entries[i].When:dd/MM HH:mm} · {entries[i].Detail}", Style = (Style)Application.Current.Resources["Cap"] });
            ActivityList.Children.Add(text);
        }
    }

    void ShowPage(FrameworkElement page)
    {
        foreach (FrameworkElement child in Pages.Children)
            child.Visibility = child == page ? Visibility.Visible : Visibility.Collapsed;
    }

    void Nav_Checked(object sender, RoutedEventArgs e)
    {
        if (!IsInitialized || sender is not RadioButton button || !nav.TryGetValue(button, out var entry)) return;
        UpdateNavIcons();
        if (busy) return;
        ShowPage(entry.Page);
        if (entry.Page == StatusPage) _ = RefreshAsync();
        else if (entry.Page == BackupsPage) _ = RenderBackupsAsync();
        else if (entry.Page == LogPage) RenderLog();
    }

    void RenderLog()
    {
        LogBox.Text = Log.ReadToday();
        LogBox.ScrollToEnd();
    }

    async Task RenderBackupsAsync()
    {
        var list = await Task.Run(Backups.List);
        BackupsList.Children.Clear();
        var head = BackupGrid();
        AddCell(head, 0, Strings.T("col_date"), "Section");
        AddCell(head, 1, Strings.T("col_reason"), "Section");
        AddCell(head, 2, Strings.T("col_plugins"), "Section");
        head.Margin = new Thickness(16, 12, 16, 0);
        BackupsList.Children.Add(head);
        if (list.Count == 0)
        {
            BackupsList.Children.Add(new TextBlock { Text = Strings.T("backups_empty"), Style = (Style)Application.Current.Resources["Cap"], Margin = new Thickness(16, 8, 16, 16) });
            return;
        }
        foreach (var backup in list)
        {
            BackupsList.Children.Add(new Border { Height = 1, Background = Res("Line"), Margin = new Thickness(12, 0, 12, 0) });
            var row = BackupGrid();
            row.Margin = new Thickness(16, 8, 16, 8);
            AddCell(row, 0, backup.When.ToString("dd/MM/yyyy HH:mm"), "Item");
            AddCell(row, 1, Strings.T("reason_" + backup.Reason), "Cap");
            AddCell(row, 2, backup.Plugins.ToString(), "MonoText");
            var restore = new Button { Content = Strings.T("btn_restore"), Style = (Style)Application.Current.Resources["Outline"], HorizontalAlignment = HorizontalAlignment.Right };
            restore.Click += (_, _) => AskRestore(backup);
            Grid.SetColumn(restore, 3);
            row.Children.Add(restore);
            BackupsList.Children.Add(row);
        }
    }

    static Grid BackupGrid()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(200) });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        return grid;
    }

    static void AddCell(Grid grid, int column, string text, string style)
    {
        var block = new TextBlock { Text = text, Style = (Style)Application.Current.Resources[style], VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0) };
        Grid.SetColumn(block, column);
        grid.Children.Add(block);
    }

    void ShowDialog(string title, string body, string ok, Action onOk, IEnumerable<RadioButton>? options = null)
    {
        DialogTitle.Text = title;
        DialogBody.Text = body;
        DialogOk.Content = ok;
        DialogOptions.Children.Clear();
        DialogOptions.Margin = new Thickness(0);
        if (options != null)
        {
            DialogOptions.Margin = new Thickness(0, 16, 0, 0);
            foreach (var option in options)
            {
                if (DialogOptions.Children.Count > 0) option.Margin = new Thickness(0, 8, 0, 0);
                DialogOptions.Children.Add(option);
            }
        }
        dialogOk = onOk;
        DialogLayer.Visibility = Visibility.Visible;
        DialogOk.Focus();
    }

    static RadioButton Option(string title, string sub, bool selected)
    {
        var text = new StackPanel();
        text.Children.Add(new TextBlock { Text = title, Style = (Style)Application.Current.Resources["Item"] });
        text.Children.Add(new TextBlock { Text = sub, Style = (Style)Application.Current.Resources["Cap"] });
        return new RadioButton { Style = (Style)Application.Current.Resources["Option"], Content = text, IsChecked = selected, GroupName = "DialogOption" };
    }

    void DialogOk_Click(object sender, RoutedEventArgs e)
    {
        DialogLayer.Visibility = Visibility.Collapsed;
        var action = dialogOk;
        dialogOk = null;
        action?.Invoke();
    }

    void DialogCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogLayer.Visibility = Visibility.Collapsed;
        dialogOk = null;
        if (snapshot != null) QuestifySwitch.IsChecked = snapshot.Questify == true;
    }

    async Task RunAsync(Operation operation, Action? after = null)
    {
        busy = true;
        NavStatus.IsChecked = true;
        ShowPage(ProgressPage);
        ProgressTitle.Text = operation.Title + "…";
        ProgressSub.Text = Strings.T("progress_sub");
        ProgressBack.Visibility = Visibility.Collapsed;
        ProgressLog.Clear();

        void OnLine(string line) => Dispatcher.BeginInvoke(new Action(() =>
        {
            ProgressLog.AppendText(line + Environment.NewLine);
            ProgressLog.ScrollToEnd();
        }));
        void OnChanged() => Dispatcher.BeginInvoke(new Action(() => RenderSteps(operation)));

        Log.Line += OnLine;
        operation.Changed += OnChanged;
        RenderSteps(operation);
        bool ok;
        try
        {
            ok = await Task.Run(operation.RunAsync);
        }
        finally
        {
            Log.Line -= OnLine;
            operation.Changed -= OnChanged;
        }
        RenderSteps(operation);
        ProgressTitle.Text = Strings.T(ok ? "progress_done" : "progress_failed");
        ProgressSub.Text = ok ? operation.ActivityTitle : operation.Error ?? "";
        ProgressBack.Visibility = Visibility.Visible;
        busy = false;
        if (ok) after?.Invoke();
    }

    void RenderSteps(Operation operation)
    {
        ProgressFill.Width = Math.Max(0, ProgressTrack.ActualWidth * operation.Progress);
        Ui.RenderSteps(StepsList, operation);
    }

    void ProgressBack_Click(object sender, RoutedEventArgs e)
    {
        ShowPage(StatusPage);
        _ = RefreshAsync();
    }

    void Check_Click(object sender, RoutedEventArgs e) => _ = RefreshAsync();

    void Fix_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(Operations.Repair(snapshot?.Problem == Problem.NotInstalled, null, true));

    void Repair_Click(object sender, RoutedEventArgs e) =>
        _ = RunAsync(Operations.Repair(snapshot?.Problem == Problem.NotInstalled, null, true));

    void Official_Click(object sender, RoutedEventArgs e) =>
        ShowDialog(Strings.T("dlg_official_title"), Strings.T("dlg_official_body"), Strings.T("dlg_official_ok"),
            () => _ = RunAsync(Operations.Official(true)));

    void Uninstall_Click(object sender, RoutedEventArgs e) => AskUninstall();

    public void AskUninstall()
    {
        var keep = Option(Strings.T("opt_keep"), Strings.T("opt_keep_sub"), true);
        var remove = Option(Strings.T("opt_remove"), Strings.T("opt_remove_sub"), false);
        ShowDialog(Strings.T("dlg_uninstall_title"), Strings.T("dlg_uninstall_body"), Strings.T("dlg_uninstall_ok"),
            () => _ = RunAsync(Operations.Uninstall(remove.IsChecked == true, true), () =>
            {
                Setup.RemoveFiles();
                Close();
            }),
            new[] { keep, remove });
    }

    void Questify_Click(object sender, RoutedEventArgs e)
    {
        if (QuestifySwitch.IsChecked == true)
        {
            ShowDialog(Strings.T("dlg_questify_title"), Strings.T("dlg_questify_body"), Strings.T("dlg_questify_ok"),
                () => _ = RunAsync(Operations.SetQuestify(true)));
        }
        else
        {
            _ = RunAsync(Operations.SetQuestify(false));
        }
    }

    void AskRestore(Backup backup) =>
        ShowDialog(Strings.T("dlg_restore_title"), Strings.T("dlg_restore_body", backup.When.ToString("dd/MM/yyyy HH:mm")), Strings.T("dlg_restore_ok"),
            () => _ = RunAsync(Operations.Restore(backup)));

    async void BackupNow_Click(object sender, RoutedEventArgs e)
    {
        BackupNowButton.IsEnabled = false;
        await Task.Run(() => Backups.Create("manual"));
        BackupNowButton.IsEnabled = true;
        await RenderBackupsAsync();
    }

    void ViewLog_Click(object sender, RoutedEventArgs e) => NavLog.IsChecked = true;

    void OpenLogs_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Paths.LogDir);
        Process.Start(new ProcessStartInfo("explorer.exe", "\"" + Paths.LogDir + "\"") { UseShellExecute = true });
    }

    void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void Max_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    void Close_Click(object sender, RoutedEventArgs e) => Close();
}
