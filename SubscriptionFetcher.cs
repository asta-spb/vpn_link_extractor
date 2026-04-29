using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace VpnLinkExtractor;

public static class SubscriptionFetcher
{
    private static readonly HttpClient Http = CreateClient();

    internal static readonly JsonSerializerOptions PrettyJson = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static readonly Regex SurrogatePairEscape = new(
        @"\\u(?<hi>[dD][89aAbB][0-9a-fA-F]{2})\\u(?<lo>[dD][cCdDeEfF][0-9a-fA-F]{2})",
        RegexOptions.Compiled);

    internal static string PrettyPrint(JsonElement element)
    {
        var raw = JsonSerializer.Serialize(element, PrettyJson);
        return SurrogatePairEscape.Replace(raw, m =>
        {
            var hi = Convert.ToInt32(m.Groups["hi"].Value, 16);
            var lo = Convert.ToInt32(m.Groups["lo"].Value, 16);
            return char.ConvertFromUtf32(0x10000 + ((hi - 0xD800) << 10) + (lo - 0xDC00));
        });
    }

    public const long MaxResponseBytes = 10 * 1024 * 1024;

    private static HttpClient CreateClient()
    {
        var handler = new HttpClientHandler { AllowAutoRedirect = true };
        var c = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(30),
            MaxResponseContentBufferSize = MaxResponseBytes,
        };
        c.DefaultRequestHeaders.UserAgent.ParseAdd("Happ/2.0");
        c.DefaultRequestHeaders.Accept.ParseAdd("application/json, text/plain, */*");
        return c;
    }

    public static async Task<List<VpnEntry>> FetchAsync(string url, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u) ||
            (u.Scheme != Uri.UriSchemeHttp && u.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("URL must use http or https scheme.");
        }

        using var resp = await Http.GetAsync(u, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();

        if (resp.Content.Headers.ContentLength is long cl && cl > MaxResponseBytes)
            throw new InvalidOperationException($"Response too large ({cl} bytes; max {MaxResponseBytes}).");

        await using var stream = await resp.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
        using var ms = new MemoryStream();
        var buffer = new byte[81920];
        int read;
        while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
        {
            if (ms.Length + read > MaxResponseBytes)
                throw new InvalidOperationException($"Response exceeds {MaxResponseBytes} bytes.");
            ms.Write(buffer, 0, read);
        }
        var body = Encoding.UTF8.GetString(ms.ToArray());
        return Parse(body);
    }

    public static List<VpnEntry> Parse(string body)
    {
        var trimmed = body.TrimStart();

        // Path 1: JSON (array of XRay configs, or single config).
        if (trimmed.StartsWith('[') || trimmed.StartsWith('{'))
        {
            return ParseJson(body);
        }

        // Path 2: base64-encoded subscription (one vless:// per line after decode).
        if (TryDecodeBase64(body, out var decoded))
        {
            return ParseLines(decoded);
        }

        // Path 3: plain-text list of vless:// links.
        return ParseLines(body);
    }

    private static List<VpnEntry> ParseJson(string json)
    {
        var result = new List<VpnEntry>();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var cfg in root.EnumerateArray())
            {
                AddConfigEntry(cfg, result);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            AddConfigEntry(root, result);
        }
        return result;
    }

    private static void AddConfigEntry(JsonElement cfg, List<VpnEntry> result)
    {
        try
        {
            var remarks = cfg.TryGetProperty("remarks", out var r) ? r.GetString() ?? "" : "";

            if (!cfg.TryGetProperty("outbounds", out var outbounds) || outbounds.ValueKind != JsonValueKind.Array)
                return;

            // Pick the first vless outbound (first in array is the active proxy in XRay configs).
            foreach (var ob in outbounds.EnumerateArray())
            {
                if (!ob.TryGetProperty("protocol", out var proto)) continue;
                if (proto.GetString() != "vless") continue;

                var uri = BuildVlessUri(ob, remarks);
                if (uri is null) continue;

                var configJson = PrettyPrint(cfg);
                result.Add(new VpnEntry(string.IsNullOrEmpty(remarks) ? ob.TryGetProperty("tag", out var t) ? t.GetString() ?? "" : "" : remarks, uri, configJson));
                return;
            }
        }
        catch
        {
            // skip malformed config — robustness over strictness
        }
    }

    private static string? BuildVlessUri(JsonElement outbound, string remarks)
    {
        if (!outbound.TryGetProperty("settings", out var settings)) return null;
        if (!settings.TryGetProperty("vnext", out var vnextArr) || vnextArr.ValueKind != JsonValueKind.Array) return null;
        if (vnextArr.GetArrayLength() == 0) return null;

        var vnext = vnextArr[0];
        if (!vnext.TryGetProperty("address", out var addrEl)) return null;
        var address = addrEl.GetString();
        if (string.IsNullOrEmpty(address)) return null;

        if (!vnext.TryGetProperty("port", out var portEl) || !portEl.TryGetInt32(out var port)) return null;
        if (port <= 0 || port > 65535) return null;

        if (!vnext.TryGetProperty("users", out var users) ||
            users.ValueKind != JsonValueKind.Array || users.GetArrayLength() == 0)
            return null;

        var user = users[0];
        if (!user.TryGetProperty("id", out var idEl)) return null;
        var id = idEl.GetString();
        if (string.IsNullOrEmpty(id)) return null;

        var encryption = user.TryGetProperty("encryption", out var e) ? e.GetString() ?? "none" : "none";
        var flow = user.TryGetProperty("flow", out var f) ? f.GetString() ?? "" : "";

        var ss = outbound.TryGetProperty("streamSettings", out var s) ? s : default;
        var network = "tcp";
        var security = "";
        var pairs = new List<(string, string)>
        {
            ("encryption", encryption)
        };

        if (ss.ValueKind == JsonValueKind.Object)
        {
            if (ss.TryGetProperty("network", out var net)) network = net.GetString() ?? "tcp";
            if (ss.TryGetProperty("security", out var sec)) security = sec.GetString() ?? "";

            if (security == "reality" && ss.TryGetProperty("realitySettings", out var rs))
            {
                AddIfPresent(pairs, "sni", rs, "serverName");
                AddIfPresent(pairs, "fp", rs, "fingerprint");
                AddIfPresent(pairs, "pbk", rs, "publicKey");
                AddIfPresent(pairs, "sid", rs, "shortId");
                AddIfPresent(pairs, "spx", rs, "spiderX");
            }
            else if (security == "tls" && ss.TryGetProperty("tlsSettings", out var ts))
            {
                AddIfPresent(pairs, "sni", ts, "serverName");
                AddIfPresent(pairs, "fp", ts, "fingerprint");
                if (ts.TryGetProperty("alpn", out var alpn) && alpn.ValueKind == JsonValueKind.Array)
                {
                    var parts = new List<string>();
                    foreach (var a in alpn.EnumerateArray())
                    {
                        var v = a.GetString();
                        if (!string.IsNullOrEmpty(v)) parts.Add(v);
                    }
                    if (parts.Count > 0) pairs.Add(("alpn", string.Join(",", parts)));
                }
            }

            // Network-specific fields.
            switch (network)
            {
                case "ws":
                    if (ss.TryGetProperty("wsSettings", out var ws))
                    {
                        AddIfPresent(pairs, "path", ws, "path");
                        if (ws.TryGetProperty("headers", out var hdr) && hdr.TryGetProperty("Host", out var host))
                        {
                            var v = host.GetString();
                            if (!string.IsNullOrEmpty(v)) pairs.Add(("host", v));
                        }
                    }
                    break;
                case "grpc":
                    if (ss.TryGetProperty("grpcSettings", out var grpc))
                    {
                        AddIfPresent(pairs, "serviceName", grpc, "serviceName");
                        if (grpc.TryGetProperty("multiMode", out var mm) && mm.GetBoolean())
                            pairs.Add(("mode", "multi"));
                    }
                    break;
                case "tcp":
                    if (ss.TryGetProperty("tcpSettings", out var tcp) &&
                        tcp.TryGetProperty("header", out var hdr2) &&
                        hdr2.TryGetProperty("type", out var ht))
                    {
                        var v = ht.GetString();
                        if (!string.IsNullOrEmpty(v) && v != "none") pairs.Add(("headerType", v));
                    }
                    break;
            }
        }

        pairs.Insert(1, ("type", network));
        if (!string.IsNullOrEmpty(security)) pairs.Insert(2, ("security", security));
        if (!string.IsNullOrEmpty(flow)) pairs.Add(("flow", flow));

        var query = string.Join("&", BuildQueryPairs(pairs));
        var fragment = string.IsNullOrEmpty(remarks) ? "" : "#" + Uri.EscapeDataString(remarks);
        return $"vless://{id}@{address}:{port}?{query}{fragment}";
    }

    private static IEnumerable<string> BuildQueryPairs(List<(string Key, string Value)> pairs)
    {
        foreach (var (k, v) in pairs)
        {
            if (string.IsNullOrEmpty(v)) continue;
            yield return $"{k}={Uri.EscapeDataString(v)}";
        }
    }

    private static void AddIfPresent(List<(string, string)> pairs, string key, JsonElement obj, string prop)
    {
        if (obj.TryGetProperty(prop, out var p))
        {
            var v = p.ValueKind == JsonValueKind.String ? p.GetString() :
                    p.ValueKind == JsonValueKind.Number ? p.GetRawText() : null;
            if (!string.IsNullOrEmpty(v)) pairs.Add((key, v));
        }
    }

    private static List<VpnEntry> ParseLines(string text)
    {
        var result = new List<VpnEntry>();
        foreach (var raw in text.Split('\n'))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line)) continue;
            if (!line.StartsWith("vless://", StringComparison.OrdinalIgnoreCase)) continue;
            var hashIdx = line.IndexOf('#');
            string name;
            if (hashIdx >= 0 && hashIdx + 1 < line.Length)
            {
                try { name = Uri.UnescapeDataString(line[(hashIdx + 1)..]); }
                catch { name = line[(hashIdx + 1)..]; }
            }
            else name = "(no name)";
            result.Add(new VpnEntry(name, line, "(no JSON config — plain-text subscription)"));
        }
        return result;
    }

    private static bool TryDecodeBase64(string input, out string decoded)
    {
        decoded = "";
        var compact = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (!char.IsWhiteSpace(ch)) compact.Append(ch);
        }
        var s = compact.ToString();
        // Base64 url-safe: replace.
        s = s.Replace('-', '+').Replace('_', '/');
        if (s.Length == 0) return false;
        var pad = s.Length % 4;
        if (pad != 0) s = s + new string('=', 4 - pad);
        try
        {
            var bytes = Convert.FromBase64String(s);
            var text = Encoding.UTF8.GetString(bytes);
            if (text.IndexOf("vless://", StringComparison.OrdinalIgnoreCase) < 0) return false;
            decoded = text;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
