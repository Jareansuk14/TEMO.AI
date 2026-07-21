using System;
using System.Security.Cryptography;
using System.Text;

namespace TEMO.AI;

internal static class AdminEndpointProtector
{
    private static readonly byte[] KeyPartA =
    {
        0x7C, 0x19, 0xA3, 0x55, 0xE2, 0x08, 0xB4, 0x6F, 0x31, 0x92, 0x4D, 0xC8, 0x17, 0xAE, 0x63, 0xF0,
        0x2B, 0x84, 0x59, 0xDC, 0x03, 0x76, 0xCB, 0x48, 0x9E, 0x11, 0xA7, 0x5A, 0xEF, 0x34, 0x88, 0x1D
    };

    private static readonly byte[] KeyPartB =
    {
        0x2A, 0x6E, 0xF1, 0x09, 0xB7, 0x43, 0xD8, 0x1C, 0x65, 0xAA, 0x0F, 0x93, 0x58, 0xED, 0x32, 0x87,
        0x4C, 0xD1, 0x26, 0x9B, 0x50, 0xE5, 0x7A, 0x0D, 0xB2, 0x47, 0xFC, 0x31, 0x86, 0x1B, 0xB0, 0x45
    };

    private static readonly byte[] KeyPartC =
    {
        0x91, 0x3E, 0xC2, 0x67, 0x14, 0xA8, 0x5D, 0xE3, 0x48, 0x7B, 0x22, 0xF6, 0x89, 0x4C, 0xB1, 0x05,
        0xDA, 0x6F, 0x33, 0x88, 0x1E, 0xC4, 0x59, 0xA2, 0x07, 0x74, 0xEB, 0x40, 0x9D, 0x2A, 0xF7, 0x6C
    };

    private static readonly byte[] KeySalt =
    {
        0xA4, 0x58, 0x2F, 0xC1, 0x76, 0x09, 0xBD, 0x33, 0xE8, 0x4A, 0x91, 0x0D, 0x62, 0xF5, 0x28, 0xCE
    };

    private static readonly byte[] HostIv =
    {
        0xA9, 0x65, 0xBE, 0x16, 0xDF, 0x33, 0xEB, 0x84, 0x19, 0x1C, 0x4D, 0x23, 0x59, 0x2F, 0x7D, 0xBF
    };

    private static readonly byte[] HostCipher =
    {
        0x4F, 0x40, 0x4C, 0xD2, 0x39, 0x07, 0x5B, 0x82, 0xEB, 0xBE, 0x1D, 0x09, 0xE1, 0xB0, 0x25, 0xB7,
        0xB9, 0x88, 0x17, 0x78, 0x68, 0xC3, 0x8F, 0x7A, 0x0A, 0x13, 0x56, 0xC2, 0x61, 0x50, 0xAA, 0xDB
    };

    private static readonly byte[] HostHash =
    {
        0xD5, 0xAD, 0x98, 0x6B, 0x52, 0x69, 0xBB, 0x1E, 0x60, 0x5A, 0x40, 0x29, 0x4F, 0x46, 0xEF, 0x87,
        0xE8, 0xD5, 0x78, 0xF7, 0xD0, 0xCA, 0xCC, 0x70, 0xFD, 0x2C, 0x4F, 0x1A, 0x3D, 0xCA, 0xB9, 0xE8
    };

    private static readonly byte[] IntegrityHash =
    {
        0x7F, 0xCE, 0x5D, 0x9D, 0x01, 0x67, 0x41, 0xB1, 0x00, 0x3A, 0xCE, 0x51, 0x85, 0x49, 0x68, 0xD8,
        0x4D, 0x87, 0x29, 0x39, 0x75, 0xBE, 0x4E, 0x71, 0x37, 0xBE, 0x35, 0x2D, 0xF3, 0xCA, 0xA9, 0xEF
    };

    private static readonly (byte Seed, byte[] Data) SegApi = (0x5A, new byte[] { 0x75, 0x01, 0x16, 0x05 });
    private static readonly (byte Seed, byte[] Data) SegAuthLogin = (0x6D, new byte[] { 0x42, 0x12, 0x0C, 0x0B, 0xED, 0xA4, 0xFD, 0xFF, 0xF1, 0xF5, 0xCC });

    private static string? _host;
    private static string? _hostDomain;
    private static string? _apiUrl;

    public static string Host => _host ??= ResolveHost();

    public static string HostDomain => _hostDomain ??= $"https://{Host}";

    public static string ApiUrl => _apiUrl ??= HostDomain + DecodeSegment(SegApi);

    public static string LoginUrl => ApiUrl + DecodeSegment(SegAuthLogin);

    private static string ResolveHost()
    {
        RuntimeGuard.EnsureSafe();

        if (!VerifyIntegrity())
            Environment.Exit(0);

        var host = UnwrapHost();
        if (!VerifyHost(host))
            Environment.Exit(0);

        return host;
    }

    private static string UnwrapHost()
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKey();
        aes.IV = HostIv;
        using var decryptor = aes.CreateDecryptor();
        var plain = decryptor.TransformFinalBlock(HostCipher, 0, HostCipher.Length);
        return Encoding.UTF8.GetString(plain);
    }

    private static bool VerifyHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
            return false;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(host));
        return CryptographicOperations.FixedTimeEquals(hash, HostHash);
    }

    private static bool VerifyIntegrity()
    {
#if DEBUG
        return true;
#else
        var blob = CollectIntegrityBlob();
        var hash = SHA256.HashData(blob);
        return CryptographicOperations.FixedTimeEquals(hash, IntegrityHash);
#endif
    }

    private static byte[] CollectIntegrityBlob()
    {
        var length = KeyPartA.Length + KeyPartB.Length + KeyPartC.Length + KeySalt.Length
            + HostIv.Length + HostCipher.Length + HostHash.Length;
        var blob = new byte[length];
        var offset = 0;
        CopyTo(KeyPartA, blob, ref offset);
        CopyTo(KeyPartB, blob, ref offset);
        CopyTo(KeyPartC, blob, ref offset);
        CopyTo(KeySalt, blob, ref offset);
        CopyTo(HostIv, blob, ref offset);
        CopyTo(HostCipher, blob, ref offset);
        CopyTo(HostHash, blob, ref offset);
        return blob;
    }

    private static void CopyTo(byte[] source, byte[] target, ref int offset)
    {
        Buffer.BlockCopy(source, 0, target, offset, source.Length);
        offset += source.Length;
    }

    private static byte[] DeriveKey()
    {
        var mixed = new byte[32];
        for (var i = 0; i < mixed.Length; i++)
            mixed[i] = (byte)(KeyPartA[i] ^ KeyPartB[i] ^ KeyPartC[i]);

        return Rfc2898DeriveBytes.Pbkdf2(mixed, KeySalt, 12000, HashAlgorithmName.SHA256, 32);
    }

    private static string DecodeSegment((byte Seed, byte[] Data) segment)
    {
        var buffer = (byte[])segment.Data.Clone();
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] ^= (byte)(segment.Seed + i * 5 + (i % 7));

        return Encoding.UTF8.GetString(buffer);
    }
}
