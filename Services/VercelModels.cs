namespace TEMO.AI;

internal sealed record VercelCliResult(int ExitCode, string Output)
{
    public bool Success => ExitCode == 0;
}

internal sealed record VercelScopeOption(string Name, string Slug, bool IsCurrent, string? TeamId = null)
{
    public string Initial => string.IsNullOrEmpty(Name) ? "?" : char.ToUpperInvariant(Name[0]).ToString();
}

internal sealed record VercelProjectOption(string Name, string Id, string? Url)
{
    public bool IsNew => string.IsNullOrEmpty(Id);
    public bool HasUrl => !IsNew && !string.IsNullOrWhiteSpace(Url);

    public string DisplayName => IsNew ? $"{Name}  [ใหม่]" : Name;

    public string Domain
    {
        get
        {
            if (IsNew) return "กด Deploy เพื่อสร้างโปรเจคนี้";
            if (string.IsNullOrWhiteSpace(Url)) return "—";
            return Url
                .Replace("https://", "", StringComparison.OrdinalIgnoreCase)
                .Replace("http://", "", StringComparison.OrdinalIgnoreCase)
                .TrimEnd('/');
        }
    }
}

internal sealed record VercelDomainAddResult(string Domain, bool Success, string Message);

internal sealed record VercelDomainActionResult(string Domain, bool Success, string Message);

internal sealed record VercelDomainDnsConfig(
    string Domain, bool IsApex, string RecordType, string RecordName, string RecordValue);

internal sealed record VercelProjectDomainOption(
    string Name,
    bool IsValidConfiguration,
    bool CanShowDns)
{
    public string ConfigurationStatus => IsValidConfiguration ? "Valid Configuration" : "Invalid Configuration";
}
