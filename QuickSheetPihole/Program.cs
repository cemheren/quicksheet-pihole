using System.Text;
using System.Text.Json;

namespace QuickSheetPihole;

class Program
{
    static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(10) };
    static PiholeStats? _cache;
    static DateTime _cacheTime = DateTime.MinValue;
    static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    static async Task Main()
    {
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("QuickSheet-PiholeExt/1.0");

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            line = line.Trim();
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var msg = JsonDocument.Parse(line).RootElement;
                var type = msg.GetProperty("type").GetString();

                if (type == "init")
                {
                    Console.WriteLine("{\"type\":\"register\",\"prefix\":\"pihole:\"}");
                }
                else if (type == "activate")
                {
                    string id = msg.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";

                    string param = "";
                    if (msg.TryGetProperty("params", out var paramsEl) && paramsEl.ValueKind == JsonValueKind.Array)
                    {
                        var parts = new List<string>();
                        foreach (var p in paramsEl.EnumerateArray())
                            parts.Add(p.GetString() ?? "");
                        param = string.Join(" ", parts).Trim();
                    }

                    var (host, token) = ParseParam(param);
                    var stats = await FetchStats(host, token);
                    var rows = RenderRows(host, stats);
                    WriteOutput(id, rows);
                }
            }
            catch { /* swallow malformed input */ }
        }
    }

    static (string host, string token) ParseParam(string param)
    {
        if (string.IsNullOrWhiteSpace(param))
            return ("pi.hole", "");

        var atIdx = param.LastIndexOf('@');
        if (atIdx > 0)
            return (param[(atIdx + 1)..].Trim(), param[..atIdx].Trim());

        return (param.Trim(), "");
    }

    static async Task<PiholeStats?> FetchStats(string host, string token)
    {
        if (_cache != null && DateTime.UtcNow - _cacheTime < CacheTtl)
            return _cache;

        try
        {
            var baseUrl = host.StartsWith("http://") || host.StartsWith("https://")
                ? host.TrimEnd('/')
                : $"http://{host}";

            var url = string.IsNullOrEmpty(token)
                ? $"{baseUrl}/admin/api.php?summaryRaw"
                : $"{baseUrl}/admin/api.php?summaryRaw&auth={Uri.EscapeDataString(token)}";

            var json = await _http.GetStringAsync(url);
            _cache = ParseStats(json);
            _cacheTime = DateTime.UtcNow;
            return _cache;
        }
        catch (Exception ex)
        {
            return new PiholeStats { Error = ex.Message.Split('\n')[0] };
        }
    }

    static PiholeStats ParseStats(string json)
    {
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        var s = new PiholeStats();

        if (root.TryGetProperty("status", out var st)) s.Status = st.GetString() ?? "";
        if (root.TryGetProperty("dns_queries_today", out var q)) s.QueriesTotal = q.GetInt64();
        if (root.TryGetProperty("ads_blocked_today", out var b)) s.QueriesBlocked = b.GetInt64();
        if (root.TryGetProperty("ads_percentage_today", out var p)) s.BlockPercent = p.GetDouble();
        if (root.TryGetProperty("domains_being_blocked", out var d)) s.DomainsBlocked = d.GetInt64();
        if (root.TryGetProperty("unique_clients", out var c)) s.UniqueClients = c.GetInt32();
        if (root.TryGetProperty("queries_cached", out var cc)) s.QueriesCached = cc.GetInt64();
        if (root.TryGetProperty("queries_forwarded", out var fwd)) s.QueriesForwarded = fwd.GetInt64();
        return s;
    }

    static List<string> RenderRows(string host, PiholeStats? stats)
    {
        var rows = new List<string>();

        if (stats == null)
        {
            rows.Add($"Pi-hole: {host}");
            rows.Add("\u26a0 No response");
            return rows;
        }

        if (!string.IsNullOrEmpty(stats.Error))
        {
            rows.Add($"Pi-hole: {host}");
            rows.Add($"\u26a0 {TruncateRight(stats.Error, 30)}");
            return rows;
        }

        var statusIcon = stats.Status?.ToLower() == "enabled" ? "\ud83d\udfe2" : "\ud83d\udd34";
        rows.Add($"Pi-hole: {host}");
        rows.Add($"Status:  {statusIcon} {(stats.Status?.ToUpper() ?? "UNKNOWN")}");
        rows.Add($"Blocked: {stats.BlockPercent:F1}%  {BlockBar(stats.BlockPercent)}");
        rows.Add($"Queries: {stats.QueriesTotal:N0} today");
        rows.Add($"Blocked: {stats.QueriesBlocked:N0}  Cached: {stats.QueriesCached:N0}");
        rows.Add($"Fwd:     {stats.QueriesForwarded:N0}");
        rows.Add($"Clients: {stats.UniqueClients} active");
        rows.Add($"Domains: {stats.DomainsBlocked:N0} blocked");
        return rows;
    }

    static string BlockBar(double pct, int width = 16)
    {
        var filled = (int)Math.Round(pct / 100.0 * width);
        filled = Math.Clamp(filled, 0, width);
        return "[" + new string('\u2588', filled) + new string('\u2591', width - filled) + "]";
    }

    static string TruncateRight(string s, int max) =>
        s.Length <= max ? s : s[..max] + "\u2026";

    static void WriteOutput(string id, List<string> rows)
    {
        var cells = new List<object>();
        for (int i = 0; i < rows.Count; i++)
        {
            cells.Add(new { r = i, c = 0, v = rows[i] });
        }

        Console.WriteLine(JsonSerializer.Serialize(new { type = "write", id, cells }));
    }
}

class PiholeStats
{
    public string Status { get; set; } = "";
    public long QueriesTotal { get; set; }
    public long QueriesBlocked { get; set; }
    public double BlockPercent { get; set; }
    public long DomainsBlocked { get; set; }
    public int UniqueClients { get; set; }
    public long QueriesCached { get; set; }
    public long QueriesForwarded { get; set; }
    public string? Error { get; set; }
}
