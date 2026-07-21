using System;
using System.Net.Http;

namespace TEMO.AI;

internal static class AdminHttpClientFactory
{
    private const string ClientType = "TEMO.AI";

    public static HttpClient Create(TimeSpan? timeout = null)
    {
        RuntimeGuard.EnsureSafe();
        EnsureDualHostLock();

        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = AdminCertificatePinning.Validate;

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = timeout ?? TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("X-Client-Type", ClientType);
        return client;
    }

    private static void EnsureDualHostLock()
    {
#if DEBUG
        return;
#else
        var endpointHost = AdminEndpointProtector.Host;
        if (!AdminCertificatePinning.IsAllowedHost(endpointHost))
            Environment.Exit(0);
#endif
    }
}
