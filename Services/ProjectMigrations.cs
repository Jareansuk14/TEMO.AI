namespace TEMO.AI;

internal static class ProjectMigrations
{
    public static void Run(string projectPath)
    {
        if (!ProjectPaths.IsProject(projectPath)) return;

        try { MigrateSeoHeadingMatcher(projectPath); }
        catch { }

        try { LegacySectionRepair.Repair(projectPath); }
        catch { }
    }

    private static void MigrateSeoHeadingMatcher(string projectPath)
    {
        var path = ProjectPaths.Src(projectPath, Path.Combine("lib", "seo-content.ts"));
        if (Io.ReadOrNull(path) is not { } content) return;

        if (content.Contains("stripLeadingBrand")) return;

        if (!content.Contains("<span[^>]*>[^<]*</span>")) return;

        var updated = content;

        if (!Regex.IsMatch(updated, @"import\s*\{[^}]*\bBRAND_NAME\b[^}]*\}\s*from\s*""@/config/site"""))
        {
            updated = Regex.Replace(updated,
                @"import\s+path\s+from\s+""node:path"";",
                m => m.Value + "\nimport { BRAND_NAME } from \"@/config/site\";",
                RegexOptions.Singleline);
        }

        updated = Regex.Replace(updated,
            @"function matchHeading\(content: string, id: string\): string \{[\s\S]*?\n\}",
            _ => NewHeadingFns,
            RegexOptions.Singleline);

        if (updated != content) Io.Write(path, updated);
    }

    private const string NewHeadingFns =
@"/** Drop leading brand from plain h1 (main-seo starts with brand name). */
function stripLeadingBrand(text: string): string {
  const trimmed = text.trim();
  if (!trimmed.startsWith(BRAND_NAME)) return trimmed;
  return trimmed.slice(BRAND_NAME.length).trimStart();
}

function matchHeading(content: string, id: string): string {
  if (!content) return """";
  const pattern = new RegExp(
    `<h1[^>]*\\bid=""${id}""[^>]*>([\\s\\S]*?)<\\/h1>`
  );
  const match = content.match(pattern);
  if (!match?.[1]) return """";
  return stripLeadingBrand(match[1].trim());
}";
}
