/*
 * QuestList Repair
 * Copyright (c) 2026 tvnclr
 * SPDX-License-Identifier: GPL-3.0-or-later
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace QuestListRepair.Core;

sealed record Release(string Repo, string Tag, string Hash, string AsarUrl, long Size);

static class Releases
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0";
        http.DefaultRequestHeaders.UserAgent.ParseAdd($"QuestListRepair/{version} (+{Paths.RepoUrl})");
        return http;
    }

    public static async Task<Release> LatestAsync(string repo, string? wanted = null)
    {
        var path = wanted == null ? "latest" : "tags/" + Uri.EscapeDataString(wanted);
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{repo}/releases/{path}");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        using var response = await Http.SendAsync(request).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new IOException(Strings.T("err_release", repo, (int)response.StatusCode));

        var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        var data = new JavaScriptSerializer { MaxJsonLength = int.MaxValue }.Deserialize<Dictionary<string, object>>(json);
        var name = (string)data["name"];
        var tag = (string)data["tag_name"];
        var asset = ((ArrayList)data["assets"]).Cast<Dictionary<string, object>>()
            .FirstOrDefault(a => (string)a["name"] == "desktop.asar")
            ?? throw new IOException(Strings.T("err_no_asset", repo));

        var last = name.Substring(name.LastIndexOf(' ') + 1);
        var hash = Regex.IsMatch(last, "^[0-9a-f]{40}$") ? last : (string)data["target_commitish"];
        return new Release(repo, tag, hash, (string)asset["browser_download_url"], Convert.ToInt64(asset["size"]));
    }

    public static async Task DownloadAsync(Release release, string destination, IProgress<double>? progress)
    {
        var temp = destination + ".download";
        using (var response = await Http.GetAsync(release.AsarUrl, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
            var total = response.Content.Headers.ContentLength ?? release.Size;
            using var input = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var output = File.Create(temp);
            var buffer = new byte[81920];
            long read = 0;
            int n;
            while ((n = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
            {
                await output.WriteAsync(buffer, 0, n).ConfigureAwait(false);
                read += n;
                if (total > 0) progress?.Report((double)read / total);
            }
        }

        var info = AsarInfo.Read(temp);
        if (info.Hash != release.Hash)
        {
            File.Delete(temp);
            throw new IOException(Strings.T("err_hash"));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination))
        {
            File.Copy(destination, destination + ".bak", true);
            File.Delete(destination);
        }
        File.Move(temp, destination);
        Log.Write($"{release.Repo} {release.Tag} saved to {destination} ({new FileInfo(destination).Length} bytes, {release.Hash})");
    }
}

sealed record AsarInfo(bool Exists, string? Hash, bool IsQuestList)
{
    static readonly byte[] QuestListMarker = Encoding.ASCII.GetBytes(Paths.Repo);

    public string ShortHash => Hash == null ? "" : Hash.Substring(0, Math.Min(7, Hash.Length));

    public static AsarInfo Read(string file)
    {
        if (!File.Exists(file)) return new(false, null, false);
        var bytes = File.ReadAllBytes(file);
        var text = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 1_000_000));
        var m = System.Text.RegularExpressions.Regex.Match(text, "// Equicord ([0-9a-f]{40})");
        return new(true, m.Success ? m.Groups[1].Value : null, IndexOf(bytes, QuestListMarker) >= 0);
    }

    static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if (haystack[i] != needle[0]) continue;
            var j = 1;
            while (j < needle.Length && haystack[i + j] == needle[j]) j++;
            if (j == needle.Length) return i;
        }
        return -1;
    }
}
