/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using QuestListRepair.Core;

namespace QuestListRepair;

public partial class SetupWindow : Window
{
    bool busy;
    bool succeeded;

    public SetupWindow()
    {
        InitializeComponent();
        Title = Strings.T("setup_title");
        TitleText.Text = Title;
        Tagline.Text = Strings.T("setup_tagline");
        QuestifyIcon.Content = Icons.Make(Icons.Zap, Ui.Res("Text"));
        QuestifyTitle.Text = Strings.T("questify_title");
        QuestifySub.Text = Strings.T("questify_sub");
        QuestifySwitch.IsChecked = EquicordSettings.IsEnabled("Questify") == true;
        LicensePre.Text = Strings.T("setup_license_pre");
        LicenseLink.Text = Strings.T("setup_license_link");
        InstallButton.Content = Strings.T("btn_install");
        ProgressTitle.Text = Strings.T("setup_installing");
        ProgressSub.Text = Strings.T("setup_installing_sub");

        Loaded += (_, _) => Ui.AnimateOrbs(Orb1Move, Orb1Scale, Orb2Move, Orb2Scale);
        Closing += (_, e) => e.Cancel = busy;
    }

    void ShowPage(FrameworkElement page)
    {
        foreach (FrameworkElement child in Pages.Children)
            child.Visibility = child == page ? Visibility.Visible : Visibility.Collapsed;
    }

    async void Install_Click(object sender, RoutedEventArgs e)
    {
        busy = true;
        CloseButton.IsEnabled = false;
        ShowPage(ProgressPage);
        var operation = Operations.Install(QuestifySwitch.IsChecked == true, Setup.ReleaseTag, false);
        void OnChanged() => Dispatcher.BeginInvoke(new Action(() => Render(operation)));
        operation.Changed += OnChanged;
        Render(operation);
        bool ok;
        try
        {
            ok = await Task.Run(operation.RunAsync);
        }
        finally
        {
            operation.Changed -= OnChanged;
            busy = false;
            CloseButton.IsEnabled = true;
        }
        Render(operation);

        succeeded = ok;
        ResultBadge.Background = Ui.Res(ok ? "SuccessSoft" : "AccentSoft");
        ResultIcon.Content = Icons.Make(ok ? Icons.Check : Icons.Alert, Ui.Res(ok ? "Success" : "AccentFg"), 36, 2.2);
        ResultTitle.Text = Strings.T(ok ? "setup_done" : "progress_failed");
        ResultBody.Text = ok ? Strings.T("setup_done_body") : operation.Error ?? "";
        SecondaryButton.Content = Strings.T(ok ? "btn_open_repair" : "btn_view_log");
        PrimaryButton.Content = Strings.T(ok ? "btn_open_discord" : "btn_retry");
        ShowPage(ResultPage);
    }

    void Render(Operation operation)
    {
        ProgressFill.Width = Math.Max(0, ProgressTrack.ActualWidth * operation.Progress);
        Ui.RenderSteps(StepsList, operation);
    }

    void Primary_Click(object sender, RoutedEventArgs e)
    {
        if (!succeeded)
        {
            Install_Click(sender, e);
            return;
        }
        var discord = Discord.Find();
        if (discord != null) Discord.Start(discord);
        Close();
    }

    void Secondary_Click(object sender, RoutedEventArgs e)
    {
        if (succeeded)
        {
            Process.Start(new ProcessStartInfo(Setup.InstalledExe) { UseShellExecute = true });
            Close();
            return;
        }
        Directory.CreateDirectory(Paths.LogDir);
        Process.Start(new ProcessStartInfo("explorer.exe", "\"" + Paths.LogDir + "\"") { UseShellExecute = true });
    }

    void Link_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    void Min_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    void Close_Click(object sender, RoutedEventArgs e) => Close();
}
