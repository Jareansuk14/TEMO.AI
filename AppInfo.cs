using System.Reflection;

namespace TEMO.AI;

public static class AppInfo
{
    public const string AppName = "TEMO.AI";

    public static string Version
    {
        get
        {
            var info = Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

            if (!string.IsNullOrWhiteSpace(info))
                return info.Split('+')[0];

            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "";
        }
    }

    public static string TitleWithVersion
        => string.IsNullOrWhiteSpace(Version) ? AppName : $"{AppName} V{Version}";
}
