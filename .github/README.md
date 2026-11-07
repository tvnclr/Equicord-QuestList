# Equicord-QuestList

<p align="center"><img src="../questlist/branding/banner.png" alt="QuestList banner" width="720"></p>

A fork of [Equicord](https://github.com/Equicord/Equicord) that ships the **QuestList** plugin and updates itself from this repository.

> **Work in progress.** There is no release yet. Until then, this README is a placeholder.

## What it is

- **QuestList** shows every Discord Quest in a single vertical list with search, opened from a button under the server list. It only lists quests and opens Discord's own buttons (Claim Reward, Launch Mission); it does not complete anything by itself.
- The rest is regular Equicord, kept in sync with upstream automatically.
- Builds are published as GitHub releases. The built-in updater points at this repository, so installs keep QuestList across updates.

## Quest automation warning

Since April 2026 Discord has been acting against accounts that complete Quests with scripts or plugins (account standing violations and temporary quest bans). QuestList does not automate quests. Equicord's **Questify** plugin does: it stays off unless you turn it on yourself, at your own risk.

## Credits and license

Equicord and Vencord are by their respective contributors. This fork is distributed under the [GNU General Public License v3.0](../LICENSE), the same license as Equicord.