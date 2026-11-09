/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace QuestListRepair;

static class Strings
{
    static readonly Dictionary<string, string> En = new()
    {
        ["app_title"] = "QuestList Repair",
        ["nav_status"] = "Status",
        ["nav_backups"] = "Backups",
        ["nav_log"] = "Log",
        ["nav_about"] = "About",
        ["ver_questlist"] = "QuestList {0}",
        ["ver_equicord"] = "Equicord {0}",

        ["status_ok"] = "All good",
        ["status_ok_sub"] = "Checked just now. QuestList updates itself from GitHub.",
        ["status_problem"] = "Needs repair",
        ["status_problem_sub"] = "Checked just now.",
        ["btn_check"] = "Check again",
        ["btn_repair_now"] = "Repair now",
        ["btn_install_now"] = "Install now",
        ["btn_view_log"] = "View log",

        ["problem_NoDiscord"] = "Discord was not found",
        ["problem_NoDiscord_sub"] = "Install Discord from discord.com and open it once, then check again.",
        ["problem_NotInstalled"] = "QuestList is not installed",
        ["problem_NotInstalled_sub"] = "This installs Equicord with QuestList into Discord.",
        ["problem_EquicordOut"] = "Equicord left Discord",
        ["problem_EquicordOut_sub"] = "Discord updated and Equicord did not come back by itself. Repairing reinstalls it and keeps your settings.",
        ["problem_Elsewhere"] = "Discord loads another Equicord build",
        ["problem_Elsewhere_sub"] = "It points at {0}. Repairing switches it to the QuestList build.",
        ["problem_BuildMissing"] = "The QuestList build is missing",
        ["problem_BuildMissing_sub"] = "equicord.asar is gone. Repairing downloads it again.",
        ["problem_OfficialBuild"] = "The installed Equicord is the official one",
        ["problem_OfficialBuild_sub"] = "That build has no QuestList. Repairing switches it to the QuestList build.",
        ["problem_PluginOff"] = "QuestList is turned off",
        ["problem_PluginOff_sub"] = "The plugin is disabled in the Equicord settings. Repairing turns it back on.",

        ["section_status"] = "STATUS",
        ["section_plugins"] = "PLUGINS",
        ["section_actions"] = "ACTIONS",
        ["section_activity"] = "RECENT ACTIVITY",
        ["section_steps"] = "STEPS",
        ["section_log"] = "LOG",

        ["tile_discord"] = "Discord",
        ["tile_equicord"] = "Equicord",
        ["tile_update"] = "Updates",
        ["tile_questlist"] = "QuestList",
        ["pill_ok"] = "Ok",
        ["pill_checking"] = "Checking…",
        ["pill_active"] = "Active",
        ["pill_on"] = "On",
        ["pill_out"] = "Out",
        ["pill_off"] = "Off",
        ["pill_stopped"] = "Stopped",
        ["pill_missing"] = "Missing",
        ["pill_other"] = "Other build",
        ["sub_not_found"] = "Not found",
        ["sub_running"] = "{0} · open",
        ["sub_equicord_in"] = "Installed in Discord",
        ["sub_equicord_out"] = "Not in Discord",
        ["sub_equicord_other"] = "Points at another build",
        ["sub_update_official"] = "From the official Equicord",
        ["sub_plugin_on"] = "Plugin enabled",
        ["sub_plugin_off"] = "Plugin disabled",

        ["questify_title"] = "Questify",
        ["questify_sub"] = "Completes quests by itself. Discord may suspend your quests for 14 days.",
        ["act_repair"] = "Repair",
        ["act_repair_sub"] = "Reinstalls Equicord and QuestList",
        ["act_official"] = "Back to the official Equicord",
        ["act_official_sub"] = "Removes QuestList, keeps the rest",
        ["act_uninstall"] = "Uninstall",
        ["act_uninstall_sub"] = "Also in Windows Apps",
        ["activity_empty"] = "Nothing here yet.",

        ["progress_sub"] = "Takes less than a minute. Discord opens by itself at the end.",
        ["progress_done"] = "Done",
        ["progress_failed"] = "That did not work",
        ["btn_back"] = "Back",

        ["backups_title"] = "Backups",
        ["backups_sub"] = "Only the Equicord settings. Nothing from your Discord account.",
        ["btn_backup_now"] = "Back up now",
        ["col_date"] = "DATE",
        ["col_reason"] = "REASON",
        ["col_plugins"] = "ACTIVE PLUGINS",
        ["btn_restore"] = "Restore",
        ["backups_foot"] = "Keeps the 10 most recent. Restoring closes Discord and opens it again.",
        ["backups_empty"] = "No backups yet.",
        ["reason_install"] = "Before installing",
        ["reason_repair"] = "Before repairing",
        ["reason_manual"] = "Made by you",
        ["reason_restore"] = "Before restoring",
        ["reason_official"] = "Before going official",
        ["reason_uninstall"] = "Before uninstalling",

        ["log_title"] = "Log",
        ["log_sub"] = "Today's record.",
        ["btn_open_folder"] = "Open folder",

        ["about_title"] = "About",
        ["about_body"] = "QuestList Repair keeps QuestList, an Equicord build, installed in your Discord.",
        ["about_version"] = "Version {0}",
        ["about_repo"] = "Repository on GitHub",
        ["about_equicord"] = "Equicord",
        ["about_license"] = "GPL-3.0 license. Inter and JetBrains Mono fonts under the SIL Open Font License.",

        ["dlg_cancel"] = "Cancel",
        ["dlg_uninstall_title"] = "Uninstall QuestList?",
        ["dlg_uninstall_body"] = "Discord closes while uninstalling. Your settings are kept in a backup.",
        ["opt_keep"] = "Back to the official Equicord",
        ["opt_keep_sub"] = "Keeps Equicord and your plugins. Only QuestList leaves.",
        ["opt_remove"] = "Remove Equicord too",
        ["opt_remove_sub"] = "Discord goes back to the original.",
        ["dlg_uninstall_ok"] = "Uninstall",
        ["dlg_official_title"] = "Back to the official Equicord?",
        ["dlg_official_body"] = "Discord restarts. QuestList leaves and Equicord updates from the official project.",
        ["dlg_official_ok"] = "Go official",
        ["dlg_questify_title"] = "Turn Questify on?",
        ["dlg_questify_body"] = "Questify completes quests by itself. Since April 2026 Discord warns the account and suspends quests for 14 days for whoever gets caught. Discord restarts to apply it.",
        ["dlg_questify_ok"] = "Turn on anyway",
        ["dlg_restore_title"] = "Restore this backup?",
        ["dlg_restore_body"] = "The Equicord settings go back to {0}. Discord restarts.",
        ["dlg_restore_ok"] = "Restore",

        ["step_close"] = "Close Discord",
        ["step_backup"] = "Back up settings",
        ["step_download"] = "Download the build",
        ["step_inject"] = "Install Equicord in Discord",
        ["step_plugins"] = "Turn on QuestList",
        ["step_start"] = "Open Discord",
        ["step_unpatch"] = "Take Equicord out of Discord",
        ["step_questify"] = "Apply the change",
        ["step_restore"] = "Restore settings",
        ["step_copy"] = "Copy QuestList Repair",
        ["setup_title"] = "QuestList Setup",
        ["setup_tagline"] = "Equicord with QuestList, right in your Discord.",
        ["setup_license_pre"] = "Discord closes while QuestList installs. By installing, you accept the ",
        ["setup_license_link"] = "GPL-3.0 license",
        ["btn_install"] = "Install",
        ["setup_installing"] = "Installing QuestList",
        ["setup_installing_sub"] = "Takes less than a minute.",
        ["setup_done"] = "All set",
        ["setup_done_body"] = "QuestList is in your Discord and updates itself. If something breaks after a Discord update, open QuestList Repair.",
        ["btn_open_repair"] = "Open QuestList Repair",
        ["btn_open_discord"] = "Open Discord",
        ["btn_retry"] = "Try again",

        ["op_install"] = "Installing",
        ["op_repair"] = "Repairing",
        ["op_official"] = "Going back to official",
        ["op_uninstall"] = "Removing Equicord",
        ["op_questify_on"] = "Turning Questify on",
        ["op_questify_off"] = "Turning Questify off",
        ["op_restore"] = "Restoring backup",

        ["activity_ok"] = "Done",
        ["activity_failed"] = "Failed: {0}",
        ["activity_installed"] = "QuestList installed",
        ["activity_repaired"] = "Repair finished",
        ["activity_official"] = "Back to the official Equicord",
        ["activity_uninstalled"] = "Equicord removed",
        ["activity_questify_on"] = "Questify turned on",
        ["activity_questify_off"] = "Questify turned off",
        ["activity_restored"] = "Backup restored",
        ["activity_updated"] = "Updated by GitHub",
        ["activity_discord_updated"] = "Discord updated",

        ["err_discord_close"] = "Could not close Discord. Quit it from the Windows tray and try again.",
        ["err_release"] = "Could not reach GitHub ({0}, code {1}).",
        ["err_no_asset"] = "The latest {0} release has no desktop.asar.",
        ["err_hash"] = "The download does not match the release. Try again.",
        ["err_no_discord"] = "Discord was not found.",
    };

    static readonly Dictionary<string, string> Pt = new()
    {
        ["app_title"] = "QuestList Repair",
        ["nav_status"] = "Status",
        ["nav_backups"] = "Backups",
        ["nav_log"] = "Log",
        ["nav_about"] = "Sobre",
        ["ver_questlist"] = "QuestList {0}",
        ["ver_equicord"] = "Equicord {0}",

        ["status_ok"] = "Tudo certo",
        ["status_ok_sub"] = "Conferido agora. O QuestList se atualiza sozinho pelo GitHub.",
        ["status_problem"] = "Precisa de reparo",
        ["status_problem_sub"] = "Conferido agora.",
        ["btn_check"] = "Conferir de novo",
        ["btn_repair_now"] = "Reparar agora",
        ["btn_install_now"] = "Instalar agora",
        ["btn_view_log"] = "Ver log",

        ["problem_NoDiscord"] = "Discord não encontrado",
        ["problem_NoDiscord_sub"] = "Instale o Discord pelo discord.com, abra uma vez e confira de novo.",
        ["problem_NotInstalled"] = "O QuestList não está instalado",
        ["problem_NotInstalled_sub"] = "Isso instala o Equicord com o QuestList no Discord.",
        ["problem_EquicordOut"] = "O Equicord saiu do Discord",
        ["problem_EquicordOut_sub"] = "O Discord se atualizou e o Equicord não voltou sozinho. O reparo reinstala e mantém as suas configurações.",
        ["problem_Elsewhere"] = "O Discord carrega outra build do Equicord",
        ["problem_Elsewhere_sub"] = "Ele aponta pra {0}. O reparo troca pela build do QuestList.",
        ["problem_BuildMissing"] = "A build do QuestList sumiu",
        ["problem_BuildMissing_sub"] = "O equicord.asar não existe mais. O reparo baixa de novo.",
        ["problem_OfficialBuild"] = "O Equicord instalado é o oficial",
        ["problem_OfficialBuild_sub"] = "Essa build não tem o QuestList. O reparo troca pela build do QuestList.",
        ["problem_PluginOff"] = "O QuestList está desligado",
        ["problem_PluginOff_sub"] = "O plugin está desativado nas configurações do Equicord. O reparo liga de novo.",

        ["section_status"] = "STATUS",
        ["section_plugins"] = "PLUGINS",
        ["section_actions"] = "AÇÕES",
        ["section_activity"] = "ATIVIDADE RECENTE",
        ["section_steps"] = "ETAPAS",
        ["section_log"] = "LOG",

        ["tile_discord"] = "Discord",
        ["tile_equicord"] = "Equicord",
        ["tile_update"] = "Atualização",
        ["tile_questlist"] = "QuestList",
        ["pill_ok"] = "Ok",
        ["pill_checking"] = "Conferindo…",
        ["pill_active"] = "Ativo",
        ["pill_on"] = "Ligada",
        ["pill_out"] = "Fora",
        ["pill_off"] = "Desligado",
        ["pill_stopped"] = "Parada",
        ["pill_missing"] = "Ausente",
        ["pill_other"] = "Outra build",
        ["sub_not_found"] = "Não encontrado",
        ["sub_running"] = "{0} · aberto",
        ["sub_equicord_in"] = "Instalado no Discord",
        ["sub_equicord_out"] = "Não está no Discord",
        ["sub_equicord_other"] = "Aponta pra outra build",
        ["sub_update_official"] = "Pelo Equicord oficial",
        ["sub_plugin_on"] = "Plugin ativado",
        ["sub_plugin_off"] = "Plugin desativado",

        ["questify_title"] = "Questify",
        ["questify_sub"] = "Completa quests sozinho. O Discord pode suspender suas quests por 14 dias.",
        ["act_repair"] = "Reparar",
        ["act_repair_sub"] = "Reinstala o Equicord e o QuestList",
        ["act_official"] = "Voltar pro Equicord oficial",
        ["act_official_sub"] = "Tira o QuestList, mantém o resto",
        ["act_uninstall"] = "Desinstalar",
        ["act_uninstall_sub"] = "Também fica em Apps do Windows",
        ["activity_empty"] = "Nada por aqui ainda.",

        ["progress_sub"] = "Leva menos de um minuto. O Discord abre sozinho no final.",
        ["progress_done"] = "Pronto",
        ["progress_failed"] = "Não deu certo",
        ["btn_back"] = "Voltar",

        ["backups_title"] = "Backups",
        ["backups_sub"] = "Só as configurações do Equicord. Nada da sua conta do Discord.",
        ["btn_backup_now"] = "Criar backup agora",
        ["col_date"] = "DATA",
        ["col_reason"] = "MOTIVO",
        ["col_plugins"] = "PLUGINS ATIVOS",
        ["btn_restore"] = "Restaurar",
        ["backups_foot"] = "Guarda os 10 mais recentes. Restaurar fecha o Discord e abre de novo.",
        ["backups_empty"] = "Nenhum backup ainda.",
        ["reason_install"] = "Antes de instalar",
        ["reason_repair"] = "Antes de reparar",
        ["reason_manual"] = "Feito por você",
        ["reason_restore"] = "Antes de restaurar",
        ["reason_official"] = "Antes de voltar pro oficial",
        ["reason_uninstall"] = "Antes de desinstalar",

        ["log_title"] = "Log",
        ["log_sub"] = "Registro de hoje.",
        ["btn_open_folder"] = "Abrir pasta",

        ["about_title"] = "Sobre",
        ["about_body"] = "O QuestList Repair mantém o QuestList, uma build do Equicord, instalado no seu Discord.",
        ["about_version"] = "Versão {0}",
        ["about_repo"] = "Repositório no GitHub",
        ["about_equicord"] = "Equicord",
        ["about_license"] = "Licença GPL-3.0. Fontes Inter e JetBrains Mono sob a SIL Open Font License.",

        ["dlg_cancel"] = "Cancelar",
        ["dlg_uninstall_title"] = "Desinstalar o QuestList?",
        ["dlg_uninstall_body"] = "O Discord fecha durante a desinstalação. As suas configurações ficam salvas em backup.",
        ["opt_keep"] = "Voltar pro Equicord oficial",
        ["opt_keep_sub"] = "Mantém o Equicord e os seus plugins. Só o QuestList sai.",
        ["opt_remove"] = "Remover o Equicord também",
        ["opt_remove_sub"] = "O Discord volta a ser o original.",
        ["dlg_uninstall_ok"] = "Desinstalar",
        ["dlg_official_title"] = "Voltar pro Equicord oficial?",
        ["dlg_official_body"] = "O Discord reinicia. O QuestList sai, e o Equicord passa a se atualizar pelo projeto oficial.",
        ["dlg_official_ok"] = "Voltar pro oficial",
        ["dlg_questify_title"] = "Ligar o Questify?",
        ["dlg_questify_body"] = "O Questify completa quests sozinho. Desde abril de 2026 o Discord dá aviso na conta e suspende as quests por 14 dias de quem é pego. O Discord reinicia pra aplicar.",
        ["dlg_questify_ok"] = "Ligar mesmo assim",
        ["dlg_restore_title"] = "Restaurar este backup?",
        ["dlg_restore_body"] = "As configurações do Equicord voltam pro estado de {0}. O Discord reinicia.",
        ["dlg_restore_ok"] = "Restaurar",

        ["step_close"] = "Fechar o Discord",
        ["step_backup"] = "Backup das configurações",
        ["step_download"] = "Baixar a build",
        ["step_inject"] = "Instalar o Equicord no Discord",
        ["step_plugins"] = "Ativar o QuestList",
        ["step_start"] = "Abrir o Discord",
        ["step_unpatch"] = "Tirar o Equicord do Discord",
        ["step_questify"] = "Aplicar a mudança",
        ["step_restore"] = "Restaurar as configurações",
        ["step_copy"] = "Copiar o QuestList Repair",
        ["setup_title"] = "Instalador do QuestList",
        ["setup_tagline"] = "O Equicord com o QuestList, direto no seu Discord.",
        ["setup_license_pre"] = "O Discord fecha durante a instalação. Ao instalar, você aceita a ",
        ["setup_license_link"] = "licença GPL-3.0",
        ["btn_install"] = "Instalar",
        ["setup_installing"] = "Instalando o QuestList",
        ["setup_installing_sub"] = "Leva menos de um minuto.",
        ["setup_done"] = "Tudo pronto",
        ["setup_done_body"] = "O QuestList já está no seu Discord e se atualiza sozinho. Se algo quebrar depois de uma atualização do Discord, abra o QuestList Repair.",
        ["btn_open_repair"] = "Abrir o QuestList Repair",
        ["btn_open_discord"] = "Abrir o Discord",
        ["btn_retry"] = "Tentar de novo",

        ["op_install"] = "Instalando",
        ["op_repair"] = "Reparando",
        ["op_official"] = "Voltando pro oficial",
        ["op_uninstall"] = "Removendo o Equicord",
        ["op_questify_on"] = "Ligando o Questify",
        ["op_questify_off"] = "Desligando o Questify",
        ["op_restore"] = "Restaurando o backup",

        ["activity_ok"] = "Concluído",
        ["activity_failed"] = "Falhou: {0}",
        ["activity_installed"] = "QuestList instalado",
        ["activity_repaired"] = "Reparo concluído",
        ["activity_official"] = "Voltou pro Equicord oficial",
        ["activity_uninstalled"] = "Equicord removido",
        ["activity_questify_on"] = "Questify ligado",
        ["activity_questify_off"] = "Questify desligado",
        ["activity_restored"] = "Backup restaurado",
        ["activity_updated"] = "Atualizado pelo GitHub",
        ["activity_discord_updated"] = "Discord atualizado",

        ["err_discord_close"] = "Não consegui fechar o Discord. Feche pela bandeja do Windows e tente de novo.",
        ["err_release"] = "Não consegui consultar o GitHub ({0}, código {1}).",
        ["err_no_asset"] = "O último release de {0} não tem o desktop.asar.",
        ["err_hash"] = "O arquivo baixado não bate com o release. Tente de novo.",
        ["err_no_discord"] = "Discord não encontrado.",
    };

    static readonly Dictionary<string, string> Current =
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "pt" ? Pt : En;

    public static bool IsPortuguese => ReferenceEquals(Current, Pt);

    public static string T(string key, params object[] args)
    {
        var text = Current.TryGetValue(key, out var value) ? value : En.TryGetValue(key, out var fallback) ? fallback : key;
        return args.Length == 0 ? text : string.Format(CultureInfo.CurrentCulture, text, args);
    }

    public static List<string> Check()
    {
        var problems = new List<string>();
        foreach (var key in En.Keys.Except(Pt.Keys)) problems.Add($"missing pt-BR: {key}");
        foreach (var key in Pt.Keys.Except(En.Keys)) problems.Add($"missing en: {key}");
        var placeholder = new Regex(@"\{\d+\}");
        foreach (var key in En.Keys.Intersect(Pt.Keys))
        {
            var a = placeholder.Matches(En[key]).Cast<Match>().Select(m => m.Value).OrderBy(x => x);
            var b = placeholder.Matches(Pt[key]).Cast<Match>().Select(m => m.Value).OrderBy(x => x);
            if (!a.SequenceEqual(b)) problems.Add($"placeholders differ: {key}");
        }
        return problems;
    }
}
