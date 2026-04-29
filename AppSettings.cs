using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace VpnLinkExtractor;

public sealed class AppSettings
{
    public const int MaxRecent = 10;
    public const int MaxUrlLength = 4096;

    public bool RememberUrls { get; set; } = true;
    public List<string> RecentUrls { get; set; } = new();
    public WindowGeometry Window { get; set; } = new();

    private static string FilePath =>
        Path.Combine(AppContext.BaseDirectory, "settings.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(FilePath)) return new AppSettings();
            var json = File.ReadAllText(FilePath);
            var s = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            Sanitize(s);
            return s;
        }
        catch
        {
            return new AppSettings();
        }
    }

    private static void Sanitize(AppSettings s)
    {
        s.RecentUrls ??= new List<string>();
        s.RecentUrls.RemoveAll(u => string.IsNullOrWhiteSpace(u) || u.Length > MaxUrlLength);
        if (s.RecentUrls.Count > MaxRecent)
            s.RecentUrls = s.RecentUrls.GetRange(0, MaxRecent);
        s.Window ??= new WindowGeometry();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, JsonOpts);
            var dir = Path.GetDirectoryName(FilePath);
            var tmp = Path.Combine(dir ?? ".", $"settings.{Guid.NewGuid():N}.tmp");
            File.WriteAllText(tmp, json);
            File.Move(tmp, FilePath, overwrite: true);
        }
        catch
        {
            // best-effort — ignore IO errors
        }
    }

    public void AddRecent(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (url.Length > MaxUrlLength) return;
        RecentUrls.RemoveAll(u => string.Equals(u, url, StringComparison.OrdinalIgnoreCase));
        RecentUrls.Insert(0, url);
        if (RecentUrls.Count > MaxRecent)
            RecentUrls.RemoveRange(MaxRecent, RecentUrls.Count - MaxRecent);
    }
}
