namespace TEMO.AI;

public static class HwidService
{
    private static readonly string FilePath = Path.Combine(Path.GetTempPath(), "temohwid.dll");

    public static string GetOrCreate()
    {
        if (File.Exists(FilePath))
            return File.ReadAllText(FilePath).Trim();

        var hwid = Guid.NewGuid().ToString("N").ToUpper();
        File.WriteAllText(FilePath, hwid);
        return hwid;
    }
}
