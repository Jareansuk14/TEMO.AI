namespace TEMO.AI;

internal static class VercelApiClient
{
    private static readonly HttpClient Http = new()
    {
        BaseAddress = new Uri("https://api.vercel.com"),
        Timeout = TimeSpan.FromSeconds(12),
    };

    public static async Task<List<VercelScopeOption>?> TryGetScopesAsync(string token)
    {
        try
        {
            var userTask = Http.SendAsync(Request(HttpMethod.Get, "/v2/user", token));
            var teamsTask = Http.SendAsync(Request(HttpMethod.Get, "/v2/teams", token));

            using var userResp = await userTask;
            if (!userResp.IsSuccessStatusCode) { (await teamsTask).Dispose(); return null; }

            var userJson = JsonNode.Parse(await userResp.Content.ReadAsStringAsync());
            var u = userJson?["user"];
            var userSlug = u?["username"]?.GetValue<string>() ?? "";
            var userName = u?["name"]?.GetValue<string>() ?? userSlug;

            var teams = new List<VercelScopeOption>();
            using var teamsResp = await teamsTask;
            if (teamsResp.IsSuccessStatusCode)
            {
                var teamsJson = JsonNode.Parse(await teamsResp.Content.ReadAsStringAsync());
                foreach (var t in teamsJson?["teams"]?.AsArray() ?? [])
                {
                    var name = t?["name"]?.GetValue<string>() ?? "";
                    var slug = t?["slug"]?.GetValue<string>() ?? "";
                    var id = t?["id"]?.GetValue<string>() ?? "";
                    if (!string.IsNullOrWhiteSpace(slug))
                        teams.Add(new VercelScopeOption(name, slug, IsCurrent: false, TeamId: id));
                }
            }

            var scopes = teams
                .Select((scope, index) => scope with { IsCurrent = index == 0 })
                .ToList();

            if (!string.IsNullOrWhiteSpace(userSlug))
                scopes.Add(new VercelScopeOption(userName, userSlug, IsCurrent: scopes.Count == 0, TeamId: null));

            return scopes.Count > 0 ? scopes : null;
        }
        catch { return null; }
    }

    public static async Task<List<VercelProjectOption>?> TryGetProjectsAsync(string token, VercelScopeOption scope)
    {
        try
        {
            var url = scope.TeamId is { Length: > 0 } teamId
                ? $"/v9/projects?limit=100&teamId={Uri.EscapeDataString(teamId)}"
                : "/v9/projects?limit=100";

            using var resp = await Http.SendAsync(Request(HttpMethod.Get, url, token));
            if (!resp.IsSuccessStatusCode) return null;

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var tasks = (json?["projects"]?.AsArray() ?? [])
                .Select(p => (Node: p, Name: p?["name"]?.GetValue<string>() ?? "", Id: p?["id"]?.GetValue<string>() ?? ""))
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .Select(async x =>
                {
                    var fallbackUrl = GetProjectUrl(x.Node);
                    var displayUrl = string.IsNullOrWhiteSpace(x.Id)
                        ? fallbackUrl
                        : await GetProjectDisplayUrlAsync(token, x.Id, fallbackUrl, scope.TeamId);
                    return new VercelProjectOption(x.Name, x.Id, displayUrl);
                });

            return (await Task.WhenAll(tasks)).ToList();
        }
        catch { return null; }
    }

    private static async Task<string?> GetProjectDisplayUrlAsync(
        string token, string projectId, string? fallbackUrl, string? teamId)
    {
        try
        {
            var url = WithTeam($"/v9/projects/{Uri.EscapeDataString(projectId)}/domains?limit=100", teamId);
            using var resp = await Http.SendAsync(Request(HttpMethod.Get, url, token));
            if (!resp.IsSuccessStatusCode) return fallbackUrl;

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var customDomains = (json?["domains"]?.AsArray() ?? [])
                .Select(d => new
                {
                    Name = d?["name"]?.GetValue<string>() ?? "",
                    Verified = d?["verified"]?.GetValue<bool>() ?? false,
                    CreatedAt = d?["createdAt"]?.GetValue<long>() ?? 0,
                })
                .Where(d => !string.IsNullOrWhiteSpace(d.Name)
                    && !d.Name.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(d => d.Verified)
                .ThenByDescending(d => d.CreatedAt)
                .ToList();

            var domain = customDomains.FirstOrDefault()?.Name;
            return string.IsNullOrWhiteSpace(domain) ? fallbackUrl : EnsureHttps(domain);
        }
        catch { return fallbackUrl; }
    }

    private static string? GetProjectUrl(JsonNode? project)
    {
        var latest = project?["latestProductionUrl"]?.GetValue<string>();
        if (!string.IsNullOrWhiteSpace(latest)) return EnsureHttps(latest);

        var aliases = project?["targets"]?["production"]?["alias"]?.AsArray();
        var alias = aliases?
            .Select(x => x?.GetValue<string>())
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var url = alias ?? project?["targets"]?["production"]?["url"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(url) ? null : EnsureHttps(url);
    }

    private static string EnsureHttps(string url) =>
        url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
        || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? url
            : $"https://{url}";

    public static async Task<VercelDomainAddResult> AddProjectDomainAsync(
        string token, string projectId, string domain, string? teamId)
    {
        try
        {
            var url = WithTeam($"/v10/projects/{Uri.EscapeDataString(projectId)}/domains", teamId);
            var req = Request(HttpMethod.Post, url, token);
            req.Content = new StringContent(
                new JsonObject { ["name"] = domain }.ToJsonString(), Encoding.UTF8, "application/json");

            using var resp = await Http.SendAsync(req);
            var text = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
                return new VercelDomainAddResult(domain, true, "เพิ่มสำเร็จ");

            var error = TryReadError(text);

            if ((int)resp.StatusCode == 400 &&
                (error?.Contains("already", StringComparison.OrdinalIgnoreCase) ?? false))
                return new VercelDomainAddResult(domain, true, "มีอยู่แล้ว");

            return new VercelDomainAddResult(domain, false, error ?? $"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return new VercelDomainAddResult(domain, false, ex.Message);
        }
    }

    public static async Task<List<VercelProjectDomainOption>?> TryGetProjectDomainsAsync(
        string token, string projectId, string? projectUrl, string? teamId)
    {
        try
        {
            var url = WithTeam($"/v9/projects/{Uri.EscapeDataString(projectId)}/domains?limit=100", teamId);
            using var resp = await Http.SendAsync(Request(HttpMethod.Get, url, token));
            if (!resp.IsSuccessStatusCode) return null;

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            var domains = (await Task.WhenAll((json?["domains"]?.AsArray() ?? [])
                .Select(d => d?["name"]?.GetValue<string>() ?? "")
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(async name =>
                {
                    var valid = await IsDomainConfiguredAsync(token, name, teamId);
                    return new VercelProjectDomainOption(name, valid, CanShowDns: !valid);
                }))).ToList();

            var vercelDomain = NormalizeHost(projectUrl);
            if (!string.IsNullOrWhiteSpace(vercelDomain)
                && vercelDomain.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
                && domains.All(d => !d.Name.Equals(vercelDomain, StringComparison.OrdinalIgnoreCase)))
            {
                domains.Add(new VercelProjectDomainOption(vercelDomain, IsValidConfiguration: true, CanShowDns: false));
            }

            return domains
                .OrderBy(d => d.Name.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch { return null; }
    }

    public static async Task<VercelDomainActionResult> DeleteProjectDomainAsync(
        string token, string projectId, string domain, string? teamId)
    {
        try
        {
            var url = WithTeam(
                $"/v9/projects/{Uri.EscapeDataString(projectId)}/domains/{Uri.EscapeDataString(domain)}",
                teamId);
            using var resp = await Http.SendAsync(Request(HttpMethod.Delete, url, token));
            var text = await resp.Content.ReadAsStringAsync();

            if (resp.IsSuccessStatusCode)
                return new VercelDomainActionResult(domain, true, "ลบสำเร็จ");

            return new VercelDomainActionResult(
                domain, false, TryReadError(text) ?? $"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return new VercelDomainActionResult(domain, false, ex.Message);
        }
    }

    public static async Task<VercelDomainDnsConfig> GetDomainConfigAsync(
        string token, string domain, bool isApex, string? teamId)
    {
        string? recommendedCname = null;
        try
        {
            var url = WithTeam($"/v6/domains/{Uri.EscapeDataString(domain)}/config", teamId);
            using var resp = await Http.SendAsync(Request(HttpMethod.Get, url, token));
            if (resp.IsSuccessStatusCode)
            {
                var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
                recommendedCname = PickRecommended(json?["recommendedCNAME"]);
            }
        }
        catch { }

        if (isApex)
            return new VercelDomainDnsConfig(domain, true, "A", "@", "76.76.21.21");

        var name = domain.Contains('.') ? domain[..domain.IndexOf('.')] : domain;
        return new VercelDomainDnsConfig(
            domain, false, "CNAME", name, recommendedCname ?? "cname.vercel-dns.com");
    }

    public static async Task<bool> IsDomainConfiguredAsync(string token, string domain, string? teamId)
    {
        if (domain.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase))
            return true;

        try
        {
            var url = WithTeam($"/v6/domains/{Uri.EscapeDataString(domain)}/config", teamId);
            using var resp = await Http.SendAsync(Request(HttpMethod.Get, url, token));
            if (!resp.IsSuccessStatusCode) return false;

            var json = JsonNode.Parse(await resp.Content.ReadAsStringAsync());
            return json?["misconfigured"]?.GetValue<bool>() == false;
        }
        catch { return false; }
    }

    private static string NormalizeHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        var value = url.Trim();
        if (!value.Contains("://")) value = $"https://{value}";

        try { return new Uri(value).Host.TrimEnd('.'); }
        catch
        {
            var slash = value.IndexOf('/');
            return (slash >= 0 ? value[..slash] : value)
                .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
                .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('.');
        }
    }

    private static string? PickRecommended(JsonNode? node)
    {
        if (node is not JsonArray arr || arr.Count == 0) return null;
        var best = arr
            .Where(x => x?["value"] is not null)
            .OrderBy(x => x?["rank"]?.GetValue<int>() ?? int.MaxValue)
            .FirstOrDefault();
        var value = best?["value"]?.GetValue<string>();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TryReadError(string text)
    {
        try { return JsonNode.Parse(text)?["error"]?["message"]?.GetValue<string>(); }
        catch { return null; }
    }

    private static string WithTeam(string url, string? teamId) =>
        teamId is { Length: > 0 } id
            ? $"{url}{(url.Contains('?') ? '&' : '?')}teamId={Uri.EscapeDataString(id)}"
            : url;

    private static HttpRequestMessage Request(HttpMethod method, string url, string token)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
        return req;
    }
}
