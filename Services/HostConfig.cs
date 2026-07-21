namespace TEMO.AI;

public static class HostConfig
{
    public static string AdminHost => AdminEndpointProtector.Host;
    public static string AdminHostDomain => AdminEndpointProtector.HostDomain;
    public static string AdminApiUrl => AdminEndpointProtector.ApiUrl;
    public static string AdminLoginUrl => AdminEndpointProtector.LoginUrl;
}
