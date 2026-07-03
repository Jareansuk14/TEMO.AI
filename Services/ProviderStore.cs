namespace TEMO.AI;

internal static class ProviderStore
{
    public static string Root => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Provider");
}
