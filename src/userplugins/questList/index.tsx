/*
 * Vencord, a Discord client mod
 * Copyright (c) 2026 Vendicated and contributors
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

/*
 * QuestList — userplugin para Equicord
 * Lista vertical única de todas as quests, sem categorias, sem carrossel, sem banner.
 *
 * Botões de cada linha:
 *  - quest que o Questify completa sozinho: o MESMO botão que o Questify coloca nos cards
 *    (texto e ação vêm de Questify.getQuestButtonProps). Aceitar, completar, retomar e parar
 *    seguem exatamente o que está configurado no Questify;
 *  - o resto (Claim Reward, Launch Mission, Play...): o botão nativo dos cards da Quest Home,
 *    renderizado aqui dentro, então faz exatamente o que o card faria;
 *  - se o botão nativo não for encontrado (update do Discord), abre a Quest Home já rolada e
 *    destacando o card da quest (/quest-home#<id>, comportamento do próprio Discord).
 *  Este plugin não tem lógica de conclusão própria, não faz chamada de API e não aplica patch.
 */

import { showNotice } from "@api/Notices";
import { isPluginEnabled, plugins } from "@api/PluginManager";
import { addServerListElement, removeServerListElement, ServerListRenderPosition } from "@api/ServerList";
import { definePluginSettings, Settings, useSettings } from "@api/Settings";
import ErrorBoundary from "@components/ErrorBoundary";
import { classNameFactory } from "@utils/css";
import { relaunch } from "@utils/native";
import definePlugin, { OptionType } from "@utils/types";
import { updateError } from "@utils/updater";
import type { Quest } from "@vencord/discord-types";
import { DefaultExtractAndLoadChunksRegex, extractAndLoadChunksLazy, filters, find, findByCodeLazy } from "@webpack";
import { closeModal, Modal, NavigationRouter, openModal, QuestStore, showToast, Tooltip, useEffect, useReducer, useRef, UserStore, useState, useStateFromStores } from "@webpack/common";

import managedStyle from "./styles.css?managed";

const cl = classNameFactory("vc-questlist-");
const QUEST_HOME = "/quest-home";

// ---------------------------------------------------------------------------
// Configurações
// ---------------------------------------------------------------------------

const settings = definePluginSettings({
    useQuestifyColors: {
        type: OptionType.BOOLEAN,
        description: "Use the tile colors and ignored list configured in Questify.",
        default: true,
    },
    showClaimed: {
        type: OptionType.BOOLEAN,
        description: "Show quests whose reward was already claimed (also toggled inside the list).",
        default: true,
    },
    showExpired: {
        type: OptionType.BOOLEAN,
        description: "Show expired quests that were never completed.",
        default: true,
    },
});

// ---------------------------------------------------------------------------
// Questify (acesso pela interface pública do plugin, sem importar arquivos internos)
// ---------------------------------------------------------------------------

interface QuestifyButtonProps { text: string; onClick: () => void | Promise<void>; }

interface QuestifyApi {
    getQuestButtonProps?(args: { quest: Quest; [key: string]: unknown; }): QuestifyButtonProps | null;
    getQuestAutoCompleteProgress?(quest: Quest): number | null;
    getActiveAutoCompletes?(): readonly { questId: string; status: string; }[];
}

function questifyApi(): QuestifyApi | null {
    try {
        if (!isPluginEnabled("Questify")) return null;
        const api = (plugins as any).Questify as QuestifyApi | undefined;
        return typeof api?.getQuestButtonProps === "function" ? api : null;
    } catch {
        return null;
    }
}

// Contexto igual ao de um card da Quest Home (location 11 = QUEST_HOME_DESKTOP), que é o que o
// Discord recebe quando o botão é clicado na página original.
const QUEST_HOME_DESKTOP = 11;

function getQuestifyButton(quest: Quest, taskType?: string): QuestifyButtonProps | null {
    try {
        return questifyApi()?.getQuestButtonProps?.({
            quest,
            taskType,
            analyticsCtxQuestContent: QUEST_HOME_DESKTOP,
            analyticsCtxSourceQuestContent: QUEST_HOME_DESKTOP,
            analyticsCtxQuestContentPosition: 0,
            analyticsCtxQuestContentRowIndex: 0,
        } as any) ?? null;
    } catch (e) {
        console.error("[QuestList] getQuestButtonProps failed", e);
        return null;
    }
}

function getLiveProgress(quest: Quest): number | null {
    try {
        return questifyApi()?.getQuestAutoCompleteProgress?.(quest) ?? null;
    } catch {
        return null;
    }
}

function hasActiveAutoCompletes(): boolean {
    try {
        return (questifyApi()?.getActiveAutoCompletes?.().length ?? 0) > 0;
    } catch {
        return false;
    }
}

// ---------------------------------------------------------------------------
// Botão nativo dos cards da Quest Home ("Claim Reward", "Launch Mission", "Play"...)
// ---------------------------------------------------------------------------

// Trecho do componente de botão dos cards; é o mesmo trecho que o Questify usa para aplicar patch nele.
const NATIVE_CTA_CODE = "config.taskConfigV2.tasks).length)return";
// Valor de QUEST_HOME_TILE_V2_FOOTER no build atual; usado só se o enum não for encontrado.
const TILE_V2_SURFACE_FALLBACK = 6;

let nativeCta: React.ComponentType<any> | null = null;
let tileSurface: number | null = null;

function getNativeCta(): React.ComponentType<any> | null {
    if (nativeCta) return nativeCta;
    try {
        nativeCta = find(filters.componentByCode(NATIVE_CTA_CODE), { isIndirect: true }) ?? null;
    } catch {
        nativeCta = null;
    }
    return nativeCta;
}

// Com a superfície dos cards novos, o "Claim Reward" abre o resgate direto em vez de mandar para a Quest Home.
function getTileSurface(): number {
    if (tileSurface != null) return tileSurface;
    try {
        const surfaces = find(filters.byProps("QUEST_HOME_TILE_V2_FOOTER", "QUEST_BAR_FOOTER"), { isIndirect: true });
        if (typeof surfaces?.QUEST_HOME_TILE_V2_FOOTER === "number") tileSurface = surfaces.QUEST_HOME_TILE_V2_FOOTER;
    } catch { /* usa o valor conhecido */ }
    return tileSurface ?? TILE_V2_SURFACE_FALLBACK;
}

// O botão fica num pedaço do código que o Discord só carrega com a Quest Home; carrega esse pedaço sem abrir a página.
const loadQuestHomeChunks = extractAndLoadChunksLazy(
    ['name:"QuestHomePage"'],
    new RegExp(`(?:${DefaultExtractAndLoadChunksRegex.source}),webpackId:\\d+,name:"QuestHomePage"`)
);

function useNativeCta(): React.ComponentType<any> | null {
    const [cta, setCta] = useState(() => getNativeCta());

    useEffect(() => {
        if (cta) return;
        let alive = true;
        loadQuestHomeChunks()
            .then(() => { if (alive) setCta(getNativeCta()); })
            .catch(e => console.error("[QuestList] Failed to load Quest Home chunks", e));
        return () => { alive = false; };
    }, []);

    return cta;
}

function questifySettings(): any {
    return (Settings.plugins as any).Questify;
}

function questifyColorsActive(): boolean {
    return settings.store.useQuestifyColors && !!questifySettings()?.enabled;
}

// ---------------------------------------------------------------------------
// Status (mesma regra do Questify: claimed > ignored > unclaimed > expired)
// ---------------------------------------------------------------------------

type Status = "UNCLAIMED" | "CLAIMED" | "IGNORED" | "EXPIRED";
type Stage = "claimable" | "progress" | "new" | "claimed" | "ignored" | "expired";

interface ColorSetting { enabled: boolean; color: number; }

// Padrões do Questify (settings/def.ts), usados quando o Questify não está disponível.
const DEFAULT_COLORS: Record<Status, ColorSetting> = {
    UNCLAIMED: { enabled: true, color: 2842239 },
    CLAIMED: { enabled: true, color: 6105983 },
    IGNORED: { enabled: true, color: 8334124 },
    EXPIRED: { enabled: true, color: 2368553 },
};
const DEFAULT_ORDER: Status[] = ["UNCLAIMED", "CLAIMED", "IGNORED", "EXPIRED"];

function getIgnoredIds(): string[] {
    if (!questifyColorsActive()) return [];
    const ids = questifySettings()?.ignoredQuestIDs?.questIDs;
    return Array.isArray(ids) ? ids : [];
}

function getColor(status: Status): number | null {
    const key = {
        UNCLAIMED: "questTileUnclaimedColor",
        CLAIMED: "questTileClaimedColor",
        IGNORED: "questTileIgnoredColor",
        EXPIRED: "questTileExpiredColor",
    }[status];

    const setting: ColorSetting | undefined = questifyColorsActive() ? questifySettings()?.[key] : DEFAULT_COLORS[status];
    const resolved = setting && typeof setting.color === "number" ? setting : DEFAULT_COLORS[status];

    return resolved.enabled ? resolved.color : null;
}

function getOrder(): Status[] {
    const order = questifyColorsActive() ? questifySettings()?.questOrder : null;
    if (!Array.isArray(order)) return DEFAULT_ORDER;

    const valid = order.filter((s: string): s is Status => DEFAULT_ORDER.includes(s as Status));
    for (const s of DEFAULT_ORDER) if (!valid.includes(s)) valid.push(s);
    return valid;
}

function isExpired(quest: Quest): boolean {
    return new Date(quest.config.expiresAt).getTime() < Date.now();
}

function getStatus(quest: Quest, ignored: string[]): Status {
    const completed = !!quest.userStatus?.completedAt;
    const expired = isExpired(quest);

    if (quest.userStatus?.claimedAt) return "CLAIMED";
    if (ignored.includes(quest.id) && (!expired || completed)) return "IGNORED";
    if (completed || !expired) return "UNCLAIMED";
    return "EXPIRED";
}

function getStage(quest: Quest, status: Status): Stage {
    switch (status) {
        case "CLAIMED": return "claimed";
        case "IGNORED": return "ignored";
        case "EXPIRED": return "expired";
    }
    if (quest.userStatus?.completedAt) return "claimable";
    if (quest.userStatus?.enrolledAt) return "progress";
    return "new";
}

// ---------------------------------------------------------------------------
// Tarefa, progresso e recompensa
// ---------------------------------------------------------------------------

interface TaskInfo { type: string; target: number; }

const VIDEO_TASKS = ["WATCH_VIDEO", "WATCH_VIDEO_ON_MOBILE"];
const TASK_PRIORITY = [
    "WATCH_VIDEO", "WATCH_VIDEO_ON_MOBILE", "PLAY_ON_DESKTOP", "PLAY_ON_DESKTOP_V2", "STREAM_ON_DESKTOP",
    "PLAY_ACTIVITY", "PLAY_ON_XBOX", "PLAY_ON_PLAYSTATION", "ACHIEVEMENT_IN_GAME", "ACHIEVEMENT_IN_ACTIVITY",
];

const TASK_LABELS: Record<string, string> = {
    WATCH_VIDEO: "Video",
    WATCH_VIDEO_ON_MOBILE: "Video (mobile)",
    PLAY_ON_DESKTOP: "Play",
    PLAY_ON_DESKTOP_V2: "Play",
    STREAM_ON_DESKTOP: "Stream",
    PLAY_ACTIVITY: "Activity",
    PLAY_ON_XBOX: "Xbox",
    PLAY_ON_PLAYSTATION: "PlayStation",
    ACHIEVEMENT_IN_GAME: "Achievement",
    ACHIEVEMENT_IN_ACTIVITY: "Achievement",
};

function getTask(quest: Quest): TaskInfo | null {
    const tasks = (quest.config.taskConfigV2?.tasks ?? {}) as Record<string, { type?: string; target?: number; }>;

    // Tarefa com progresso mais recente
    const entries = Object.entries(quest.userStatus?.progress ?? {}) as [string, any][];
    entries.sort(([, a], [, b]) =>
        new Date(b?.heartbeat?.lastBeatAt ?? b?.updatedAt ?? 0).getTime()
        - new Date(a?.heartbeat?.lastBeatAt ?? a?.updatedAt ?? 0).getTime());

    for (const [key, entry] of entries) {
        const type = entry?.eventName ?? key;
        const task = tasks[type];
        if (task?.target) return { type, target: task.target };
    }

    for (const type of TASK_PRIORITY) {
        const task = tasks[type];
        if (task?.target) return { type, target: task.target };
    }

    const first = Object.entries(tasks)[0];
    return first?.[1]?.target ? { type: first[0], target: first[1].target } : null;
}

function getProgressValue(quest: Quest, task: TaskInfo): number {
    const live = getLiveProgress(quest);
    if (live != null) return live;

    const progress = quest.userStatus?.progress ?? {};
    const store = QuestStore as any;

    const read = (type: string): number => {
        const optimistic = typeof store.getOptimisticProgress === "function"
            ? store.getOptimisticProgress(quest.id, type)
            : undefined;
        return Math.max(optimistic ?? 0, progress[type]?.value ?? 0);
    };

    return VIDEO_TASKS.includes(task.type)
        ? Math.max(...VIDEO_TASKS.map(read))
        : read(task.type);
}

function isTimeTask(type: string): boolean {
    return !type.startsWith("ACHIEVEMENT");
}

function formatDuration(seconds: number): string {
    const m = Math.round(seconds / 60);
    return m >= 1 ? `${m} min` : `${Math.round(seconds)} s`;
}

// Mesma função que o Discord (e o Questify) usam para calcular Orbs, incluindo o bônus do Nitro.
const getQuestOrbQuantity = findByCodeLazy("premiumOrbQuantity??", "orbQuantity") as (config: Quest["config"], user: unknown) => number | null;

function getReward(quest: Quest): string {
    const reward = quest.config.rewardsConfig?.rewards?.[0] as any;
    if (!reward) return "—";

    if (typeof reward.orbQuantity === "number") {
        let orbs: number = reward.orbQuantity;
        try {
            const withBonus = getQuestOrbQuantity(quest.config, UserStore.getCurrentUser());
            if (typeof withBonus === "number" && withBonus > 0) orbs = withBonus;
        } catch { /* fica com o valor base */ }
        return `${orbs.toLocaleString()} Orbs`;
    }

    return reward.messages?.name ?? "—";
}

const rtf = new Intl.RelativeTimeFormat("en", { numeric: "auto" });

function relative(date: string | null | undefined): string {
    if (!date) return "";
    const diff = new Date(date).getTime() - Date.now();
    const abs = Math.abs(diff);
    const minute = 60_000, hour = 60 * minute, day = 24 * hour;

    if (abs >= day) return rtf.format(Math.round(diff / day), "day");
    if (abs >= hour) return rtf.format(Math.round(diff / hour), "hour");
    return rtf.format(Math.round(diff / minute), "minute");
}

function shortDate(date: string | null | undefined): string {
    return date ? new Date(date).toLocaleDateString(undefined, { day: "2-digit", month: "2-digit" }) : "";
}

// ---------------------------------------------------------------------------
// Imagem do jogo (formato do asset varia; tenta URLs em sequência e some se todas falharem)
// ---------------------------------------------------------------------------

function assetCandidates(quest: Quest): string[] {
    const asset = quest.config.assets?.gameTileDark || quest.config.assets?.gameTile;
    if (!asset) return [];
    if (/^https?:\/\//.test(asset)) return [asset];

    const clean = asset.replace(/^\/+/, "");
    const cdn = "https://cdn.discordapp.com";
    const list = [
        `${cdn}/assets/quests/${quest.id}/${clean}`,
        `${cdn}/quests/${quest.id}/${clean}`,
    ];
    if (clean.includes("/")) list.unshift(`${cdn}/${clean}`, `${cdn}/assets/${clean}`);
    return list;
}

function GameTile({ quest }: { quest: Quest; }) {
    const candidates = assetCandidates(quest);
    const [index, setIndex] = useState(0);

    if (index >= candidates.length) return <div className={cl("tile")} />;

    return (
        <img
            className={cl("tile")}
            src={candidates[index]}
            alt=""
            loading="lazy"
            onError={() => setIndex(i => i + 1)}
        />
    );
}

// ---------------------------------------------------------------------------
// Linha
// ---------------------------------------------------------------------------

function toRGB(color: number): string {
    return `${(color >> 16) & 0xff} ${(color >> 8) & 0xff} ${color & 0xff}`;
}

interface Row { quest: Quest; status: Status; stage: Stage; }

function openQuestHome(close: () => void, questId?: string) {
    close();
    // Com #<id>, o próprio Discord rola a Quest Home até o card da quest e destaca ele.
    NavigationRouter.transitionTo(questId ? `${QUEST_HOME}#${questId}` : QUEST_HOME);
}

const ACTIVITY_TASKS = ["PLAY_ACTIVITY", "ACHIEVEMENT_IN_ACTIVITY"];

interface ActionProps { row: Row; close: () => void; refresh: () => void; NativeCta: React.ComponentType<any> | null; }

function FallbackAction({ row, close }: { row: Row; close: () => void; }) {
    const { quest, stage } = row;
    if (stage === "claimable") {
        return <button className={cl("action", "action-claim")} title="Opens this quest on Quest Home" onClick={() => openQuestHome(close, quest.id)}>Claim ↗</button>;
    }
    return <button className={cl("action", "action-open")} title="Opens this quest on Quest Home" onClick={() => openQuestHome(close, quest.id)}>Open ↗</button>;
}

function NativeAction({ row, close, Cta }: { row: Row; close: () => void; Cta: React.ComponentType<any>; }) {
    const { quest, stage } = row;
    const task = getTask(quest)?.type ?? "";
    // Atividade (Launch Mission) abre por trás do modal; fecha a lista para ela aparecer.
    const closeAfterClick = stage !== "claimable" && ACTIVITY_TASKS.includes(task);

    return (
        <div
            className={cl("native")}
            onClickCapture={() => { if (closeAfterClick) setTimeout(close, 0); }}
        >
            <ErrorBoundary noop fallback={() => <FallbackAction row={row} close={close} />}>
                <Cta
                    quest={quest}
                    surface={getTileSurface()}
                    size="sm"
                    analyticsCtxQuestContent={QUEST_HOME_DESKTOP}
                    analyticsCtxSourceQuestContent={QUEST_HOME_DESKTOP}
                    analyticsCtxQuestContentPosition={0}
                    analyticsCtxQuestContentRowIndex={0}
                />
            </ErrorBoundary>
        </div>
    );
}

function QuestAction({ row, close, refresh, NativeCta }: ActionProps) {
    const { quest, stage } = row;
    const [busy, setBusy] = useState(false);

    // 1) Botão do Questify: mesmo texto e mesma ação do card da página de quests.
    const questify = (stage === "new" || stage === "progress") ? getQuestifyButton(quest, getTask(quest)?.type) : null;
    if (questify) {
        const running = /^(Completing|Queued|Resuming)/.test(questify.text);
        return (
            <button
                className={cl("action", running ? "action-running" : "action-start")}
                disabled={busy}
                title={running ? "Click to stop (Questify)" : "Start with Questify"}
                onClick={async () => {
                    setBusy(true);
                    try {
                        await questify.onClick();
                    } catch (e) {
                        console.error("[QuestList] Questify action failed", e);
                        showToast({ text: `Questify: ${e instanceof Error ? e.message : "action failed"}`, variant: "critical" });
                    } finally {
                        setBusy(false);
                        refresh();
                    }
                }}
            >
                {questify.text}
            </button>
        );
    }

    // 2) Sem suporte do Questify para essa quest: botão nativo do card (Claim Reward, Launch Mission...).
    //    Se ele não existir neste build do Discord, abre a quest na Quest Home.
    if (stage === "claimable" || stage === "new" || stage === "progress") {
        return NativeCta
            ? <NativeAction row={row} close={close} Cta={NativeCta} />
            : <FallbackAction row={row} close={close} />;
    }

    // 3) Sem ação: só o status.
    const label = stage === "claimed" ? "Claimed" : stage === "ignored" ? "Ignored" : "Expired";
    return <div className={cl("status", `status-${stage}`)}>{label}</div>;
}

function QuestRow({ row, close, refresh, NativeCta }: ActionProps) {
    const { quest, status, stage } = row;
    const task = getTask(quest);
    const color = getColor(status);

    const value = task ? getProgressValue(quest, task) : 0;
    const percent = task && task.target
        ? Math.min(100, Math.floor(((stage === "claimable" || stage === "claimed") ? task.target : value) / task.target * 100))
        : 0;

    const taskText = task
        ? `${TASK_LABELS[task.type] ?? task.type}${isTimeTask(task.type) ? ` · ${formatDuration(task.target)}` : ""}`
        : "";

    let detail: string;
    switch (stage) {
        case "claimed":
            detail = `claimed ${shortDate(quest.userStatus?.claimedAt)}`;
            break;
        case "claimable":
            detail = `completed ${shortDate(quest.userStatus?.completedAt)} · reward expires ${relative(quest.config.rewardsConfig?.rewardsExpireAt)}`;
            break;
        case "progress":
            detail = `${percent}% · ends ${relative(quest.config.expiresAt)}`;
            break;
        case "expired":
            detail = `ended ${relative(quest.config.expiresAt)}`;
            break;
        default:
            detail = `ends ${relative(quest.config.expiresAt)}`;
    }

    const style: Record<string, string> = color != null ? { "--vc-questlist-color": toRGB(color) } : {};

    return (
        <div className={cl("row", `row-${stage}`, color != null && "row-colored")} style={style as any}>
            <GameTile quest={quest} />
            <div className={cl("main")}>
                <div className={cl("name")} title={quest.config.messages?.questName}>
                    {quest.config.messages?.gameTitle || quest.config.messages?.questName}
                </div>
                <div className={cl("sub")}>
                    {[taskText, detail].filter(Boolean).join(" · ")}
                </div>
            </div>
            <div className={cl("reward")}>{getReward(quest)}</div>
            <QuestAction row={row} close={close} refresh={refresh} NativeCta={NativeCta} />
            <button
                className={cl("jump")}
                title="Show on Quest Home"
                aria-label="Show on Quest Home"
                onClick={() => openQuestHome(close, quest.id)}
            >
                ↗
            </button>
            {stage === "progress" && percent > 0 && (
                <div className={cl("bar")}><div className={cl("bar-fill")} style={{ width: `${percent}%` }} /></div>
            )}
        </div>
    );
}

// ---------------------------------------------------------------------------
// Lista
// ---------------------------------------------------------------------------

const STAGE_RANK: Record<Stage, number> = { claimable: 0, progress: 1, new: 2, claimed: 3, ignored: 4, expired: 5 };

function normalize(text: string): string {
    return text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();
}

function matchesSearch(quest: Quest, search: string): boolean {
    if (!search) return true;
    const m = quest.config.messages ?? ({} as any);
    const haystack = normalize([m.gameTitle, m.questName, m.gamePublisher, quest.config.application?.name].filter(Boolean).join(" "));
    return normalize(search).split(/\s+/).filter(Boolean).every(word => haystack.includes(word));
}

function buildRows(quests: Quest[], search: string): Row[] {
    const ignored = getIgnoredIds();
    const order = getOrder();
    const { showClaimed, showExpired } = settings.store;

    const rows = quests
        .filter(q => !q.preview && matchesSearch(q, search))
        .map(quest => {
            const status = getStatus(quest, ignored);
            return { quest, status, stage: getStage(quest, status) };
        })
        // Na busca, mostra tudo o que bate, inclusive resgatadas/expiradas.
        .filter(r => search || ((showClaimed || r.status !== "CLAIMED") && (showExpired || r.status !== "EXPIRED")));

    const time = (d?: string | null) => (d ? new Date(d).getTime() : 0);

    return rows.sort((a, b) => {
        const byStatus = order.indexOf(a.status) - order.indexOf(b.status);
        if (byStatus) return byStatus;

        const byStage = STAGE_RANK[a.stage] - STAGE_RANK[b.stage];
        if (byStage) return byStage;

        switch (a.stage) {
            case "claimed":
                return time(b.quest.userStatus?.claimedAt) - time(a.quest.userStatus?.claimedAt);
            case "expired":
                return time(b.quest.config.expiresAt) - time(a.quest.config.expiresAt);
            default:
                return time(a.quest.config.expiresAt) - time(b.quest.config.expiresAt);
        }
    });
}

function QuestListModal({ modalProps, modalKey }: { modalProps: any; modalKey: () => string; }) {
    const { showClaimed } = settings.use(["useQuestifyColors", "showClaimed", "showExpired"]);
    useSettings(["plugins.Questify.*"] as any);

    const quests = useStateFromStores([QuestStore], () => Array.from(QuestStore.quests.values()));
    const NativeCta = useNativeCta();
    const [search, setSearch] = useState("");
    const searchRef = useRef<HTMLInputElement>(null);

    // O modal do Discord segura o foco ao abrir; foca a busca logo depois.
    useEffect(() => {
        const t = setTimeout(() => searchRef.current?.focus(), 150);
        return () => clearTimeout(t);
    }, []);
    const [, refresh] = useReducer((n: number) => n + 1, 0);

    // Enquanto o Questify estiver completando algo, atualiza a cada 1 s (contagem regressiva);
    // fora isso, a cada 30 s (tempos relativos).
    useEffect(() => {
        let ticks = 0;
        const id = setInterval(() => {
            ticks++;
            if (hasActiveAutoCompletes() || ticks % 30 === 0) refresh();
        }, 1000);
        return () => clearInterval(id);
    }, []);

    const rows = buildRows(quests, search);
    const all = buildRows(quests, "");
    const count = (stage: Stage) => all.filter(r => r.stage === stage).length;
    const claimedTotal = quests.filter(q => !q.preview && q.userStatus?.claimedAt).length;
    const summary = [
        `${count("claimable")} to claim`,
        `${count("progress")} in progress`,
        `${count("new")} not started`,
        `${claimedTotal} claimed`,
    ].join(" · ");

    const close = () => closeModal(modalKey());

    return (
        <Modal
            {...modalProps}
            size="lg"
            title="Quests"
            subtitle={summary}
            actions={[
                { text: "Open Quest Home", variant: "secondary", onClick: () => openQuestHome(close) },
                { text: "Close", variant: "primary", onClick: modalProps.onClose },
            ]}
        >
            <div className={cl("toolbar")}>
                <input
                    className={cl("search")}
                    type="search"
                    placeholder="Search game or publisher…"
                    ref={searchRef}
                    value={search}
                    onChange={e => setSearch(e.currentTarget.value)}
                />
                <button
                    className={cl("toggle", !showClaimed && "toggle-on")}
                    onClick={() => { settings.store.showClaimed = !showClaimed; }}
                >
                    {showClaimed ? "Hide claimed" : "Show claimed"}
                </button>
            </div>
            <div className={cl("list")}>
                {rows.length === 0
                    ? (
                        <div className={cl("empty")}>
                            {search
                                ? `No quest matches “${search}”.`
                                : "No quests loaded yet. Open Quest Home once so Discord fetches them."}
                        </div>
                    )
                    : rows.map(row => <QuestRow key={row.quest.id} row={row} close={close} refresh={refresh} NativeCta={NativeCta} />)}
            </div>
        </Modal>
    );
}

function openQuestList() {
    let key = "";
    key = openModal(modalProps => (
        <ErrorBoundary>
            <QuestListModal modalProps={modalProps} modalKey={() => key} />
        </ErrorBoundary>
    ));
}

// ---------------------------------------------------------------------------
// Botão na lista de servidores
// ---------------------------------------------------------------------------

function ListIcon() {
    return (
        <svg viewBox="0 0 24 24" width="24" height="24" fill="currentColor" aria-hidden="true">
            <path d="M4 5.5A1.5 1.5 0 1 1 4 8.5 1.5 1.5 0 0 1 4 5.5Zm4 .5h13v2H8V6Zm-4 4.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm4 .5h13v2H8v-2Zm-4 4.5a1.5 1.5 0 1 1 0 3 1.5 1.5 0 0 1 0-3Zm4 .5h13v2H8v-2Z" />
        </svg>
    );
}

// Contador: quests que ainda dá pra fazer ou resgatar (não começadas, em andamento e completas
// esperando resgate). Resgatadas, expiradas e ignoradas no Questify ficam de fora, igual na lista.
function isAvailable(quest: Quest, ignored: string[]): boolean {
    if (quest.preview || getStatus(quest, ignored) !== "UNCLAIMED") return false;

    // Completa, mas o prazo de resgate já venceu: não tem mais o que fazer com ela.
    const rewardsExpireAt = quest.config.rewardsConfig?.rewardsExpireAt;
    return !(quest.userStatus?.completedAt && rewardsExpireAt && new Date(rewardsExpireAt).getTime() < Date.now());
}

function countAvailable(): number {
    const ignored = getIgnoredIds();
    let count = 0;
    for (const quest of QuestStore.quests.values()) {
        if (isAvailable(quest, ignored)) count++;
    }
    return count;
}

const BUTTON_SIZE = 40;
const BADGE_HEIGHT = 16;
const BADGE_GAP = 3;
const QUESTIFY_SETTINGS = ["plugins.Questify.*"] as any;

function badgeWidth(label: string): number {
    return label.length <= 1 ? 16 : label.length === 2 ? 22 : 28;
}

// Recorte em volta do contador, como o Discord faz nos ícones de servidor: tira o fundo do botão num
// retângulo arredondado 3 px maior que o contador, então ele continua separado mesmo com o botão azul.
function badgeMask(width: number): string {
    const r = BADGE_HEIGHT / 2 + BADGE_GAP;
    const left = BUTTON_SIZE - width - BADGE_GAP;
    const right = BUTTON_SIZE + BADGE_GAP;
    const top = BUTTON_SIZE - BADGE_HEIGHT - BADGE_GAP;
    const bottom = BUTTON_SIZE + BADGE_GAP;
    const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${BUTTON_SIZE}" height="${BUTTON_SIZE}">`
        + `<path fill-rule="evenodd" d="M0 0H${BUTTON_SIZE}V${BUTTON_SIZE}H0Z M${left + r} ${top}H${right - r}`
        + `A${r} ${r} 0 0 1 ${right - r} ${bottom}H${left + r}A${r} ${r} 0 0 1 ${left + r} ${top}Z"/></svg>`;
    return `url("data:image/svg+xml,${encodeURIComponent(svg)}")`;
}

// O Discord muda o layout da coluna de servidores de tempos em tempos, e o botão sai do centro. Em vez de
// depender do layout, mede o centro do botão do Discord logo acima e desloca o nosso para o mesmo eixo.
function useAlignWithHome(wrapRef: React.RefObject<HTMLDivElement | null>) {
    useEffect(() => {
        const wrap = wrapRef.current;
        if (!wrap) return;

        let shift = 0;
        const align = () => {
            const reference = document.querySelector<HTMLElement>('[data-list-item-id="guildsnav___home"]')
                ?? document.querySelector<HTMLElement>('[data-list-item-id^="guildsnav___"]');
            const holder = wrap.firstElementChild as HTMLElement | null;
            if (!reference || !holder) return;

            const ref = reference.getBoundingClientRect();
            const own = holder.getBoundingClientRect();
            if (!ref.width || !own.width) return;

            const next = Math.round(ref.left + ref.width / 2 - (own.left - shift + own.width / 2));
            if (next !== shift && Math.abs(next) <= 24) {
                shift = next;
                wrap.style.transform = shift ? `translateX(${shift}px)` : "";
            }
        };

        const frame = requestAnimationFrame(align);
        const observer = new ResizeObserver(align);
        observer.observe(wrap.parentElement ?? wrap);
        window.addEventListener("resize", align);

        return () => {
            cancelAnimationFrame(frame);
            observer.disconnect();
            window.removeEventListener("resize", align);
        };
    }, []);
}

function QuestListButton() {
    const wrapRef = useRef<HTMLDivElement>(null);
    useAlignWithHome(wrapRef);
    useSettings(QUESTIFY_SETTINGS);
    const { useQuestifyColors } = settings.use(["useQuestifyColors"]);

    // Recalcula a cada minuto para tirar do número as quests que expiram.
    const [tick, setTick] = useState(0);
    useEffect(() => {
        const id = setInterval(() => setTick(t => t + 1), 60_000);
        return () => clearInterval(id);
    }, []);

    const ignoredKey = getIgnoredIds().join(",");
    const count = useStateFromStores([QuestStore], countAvailable, [ignoredKey, useQuestifyColors, tick]);

    const label = count > 99 ? "99+" : String(count);
    const width = badgeWidth(label);
    const mask = count > 0 ? badgeMask(width) : null;

    return (
        <div className={cl("button-wrap")} ref={wrapRef}>
            <Tooltip text="Quest List" position="right">
                {tooltipProps => (
                    <div
                        {...tooltipProps}
                        className={cl("button-holder")}
                        role="button"
                        tabIndex={0}
                        aria-label={count > 0 ? `Quest List, ${count} available` : "Quest List"}
                        onClick={openQuestList}
                        onKeyDown={e => (e.key === "Enter" || e.key === " ") && openQuestList()}
                    >
                        <div className={cl("button")} style={mask ? { WebkitMaskImage: mask, maskImage: mask } : undefined}>
                            <ListIcon />
                        </div>
                        {count > 0 && <span className={cl("badge")} style={{ width }}>{label}</span>}
                    </div>
                )}
            </Tooltip>
        </div>
    );
}

const updateRetryMinutes = [1, 5, 15, 30];
let updateRetryTimer: ReturnType<typeof setTimeout> | undefined;

async function retryUpdate(attempt: number) {
    if (updateError !== undefined) {
        const fetched = await VencordNative.updater.fetchUpdate();
        if (fetched.ok && !fetched.value) return;
        if (fetched.ok && (await VencordNative.updater.applyUpdate()).ok) {
            if (Settings.autoUpdateNotification) showNotice("Equicord has been updated!", "Restart", relaunch);
            return;
        }
    }
    if (++attempt < updateRetryMinutes.length) updateRetryTimer = setTimeout(retryUpdate, updateRetryMinutes[attempt] * 60_000, attempt);
}

export default definePlugin({
    name: "QuestList",
    description: "Vertical list of every quest with search. Rows use Questify's button for auto-complete and Discord's own quest buttons (Claim Reward, Launch Mission...) for the rest. No Discord patches.",
    tags: ["Utility", "Organisation"],
    authors: [{ name: "tvnclr", id: 0n }],
    dependencies: ["ServerListAPI"],
    settings,
    managedStyle,

    renderButton: ErrorBoundary.wrap(QuestListButton, { noop: true }),

    start() {
        addServerListElement(ServerListRenderPosition.Above, this.renderButton);
        if (!IS_DEV && !IS_WEB && !IS_UPDATER_DISABLED && Settings.autoUpdate)
            updateRetryTimer = setTimeout(retryUpdate, updateRetryMinutes[0] * 60_000, 0);
    },

    stop() {
        removeServerListElement(ServerListRenderPosition.Above, this.renderButton);
        clearTimeout(updateRetryTimer);
    },
});
