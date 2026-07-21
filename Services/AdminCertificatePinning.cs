using System;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace TEMO.AI;

internal static class AdminCertificatePinning
{
    private const byte PinMask = 0xC3;
    private const byte HostMask = 0x7E;

    private static readonly byte[] SpkiPin =
    {
        0xA1, 0xAA, 0xE0, 0xD3, 0xD9, 0x98, 0x35, 0x75, 0x28, 0x16, 0x57, 0x0F, 0x24, 0x6A, 0x6B, 0x5F,
        0x11, 0x1A, 0xEB, 0xF6, 0xAA, 0x9B, 0x83, 0xF5, 0xFA, 0xEF, 0xD5, 0x8D, 0x96, 0x3B, 0x38, 0x29,
        0x42, 0x1E, 0x0D, 0x72, 0x2B, 0x68, 0x03, 0x42, 0x43, 0xE0, 0xF7, 0xA8, 0xC2, 0x84, 0xDB, 0xAC,
        0xB0, 0xBB, 0xD0, 0xCC, 0x99, 0x38, 0x23, 0x19, 0x49, 0x0E, 0x20, 0x7F, 0x36, 0x55, 0x5B, 0x1E
    };

    private static readonly byte[] AllowedHostHash =
    {
        0xAB, 0xD3, 0xE6, 0x15, 0x2C, 0x17, 0xC5, 0x60, 0x1E, 0x24, 0x3E, 0x57, 0x31, 0x38, 0x91, 0xF9,
        0x96, 0xAB, 0x06, 0x89, 0xAE, 0xB4, 0xB2, 0x0E, 0x83, 0x52, 0x31, 0x64, 0x43, 0xB4, 0xC7, 0x96
    };

    internal static bool IsAllowedHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(host));
        var expected = DecodeAllowedHostHash();
        return CryptographicOperations.FixedTimeEquals(hash, expected);
    }

    public static bool Validate(HttpRequestMessage? request, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors errors)
    {
#if DEBUG
        return errors == SslPolicyErrors.None;
#else
        RuntimeGuard.EnsureSafe();

        if (certificate is null || errors != SslPolicyErrors.None)
            return false;

        if (!MatchesRequestHost(request, certificate))
            return false;

        return VerifySpkiPin(certificate);
#endif
    }

    private static bool MatchesRequestHost(HttpRequestMessage? request, X509Certificate2 certificate)
    {
        if (request?.RequestUri?.Host is not { Length: > 0 } requestHost)
            return false;

        if (!IsAllowedHost(requestHost))
            return false;

        return certificate.MatchesHostname(requestHost);
    }

    private static bool VerifySpkiPin(X509Certificate2 certificate)
    {
        var spki = certificate.PublicKey.EncodedKeyValue.RawData;
        var hash = SHA256.HashData(spki);
        var expectedHex = DecodePin();
        var actualHex = Convert.ToHexString(hash).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHex),
            Encoding.UTF8.GetBytes(expectedHex));
    }

    private static byte[] DecodeAllowedHostHash()
    {
        var buffer = (byte[])AllowedHostHash.Clone();
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] ^= HostMask;

        return buffer;
    }

    private static string DecodePin()
    {
        var buffer = (byte[])SpkiPin.Clone();
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] ^= (byte)(PinMask + i * 11);

        return Encoding.UTF8.GetString(buffer);
    }
}
