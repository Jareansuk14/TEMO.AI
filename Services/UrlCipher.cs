using System.Runtime.CompilerServices;

namespace TEMO.AI;

internal static class UrlCipher
{
    private static ReadOnlySpan<byte> EncKey => new byte[]
    {
        0xE9, 0xCB, 0xCA, 0x80, 0xE4, 0xC6, 0xDB, 0x94,
        0xEB, 0xA1, 0xB6, 0xAA, 0xBD, 0xE2, 0xFD, 0xE2,
        0xE7, 0xEE
    };

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static string Decode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        var key = ResolveKey();
        var buffer = new byte[data.Length];
        for (var i = 0; i < data.Length; i++)
            buffer[i] = (byte)(data[i] ^ key[i % key.Length]);

        return Encoding.UTF8.GetString(buffer);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte[] ResolveKey()
    {
        var enc = EncKey;
        var key = new byte[enc.Length];
        for (var i = 0; i < enc.Length; i++)
            key[i] = (byte)(enc[i] ^ (byte)(0xA5 + (i * 3 & 0xFF)));
        return key;
    }
}
