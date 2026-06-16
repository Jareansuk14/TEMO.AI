namespace TEMO.AI;

internal enum FieldKind { Pattern, Brand, Faq }

internal sealed record FieldDef(
    string Id,
    string Label,
    string Section,
    string RelFile,
    string Pattern,
    FieldKind Kind = FieldKind.Pattern,
    bool Multi = false)
{
    public bool IsPattern => Kind == FieldKind.Pattern && !string.IsNullOrEmpty(Pattern);
}
