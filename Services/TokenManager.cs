namespace TEMO.AI;

public static class TokenManager
{
    private static readonly object Lock = new();
    private static string? _cachedToken;
    private static string? _cachedUsername;
    private static string? _cachedRole;

    private static string TokenFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "TEMO.AI",
            "token.json");

    public static void SetToken(string token, string? username = null, string? role = null)
    {
        lock (Lock)
        {
            _cachedToken = token;
            _cachedUsername = username ?? _cachedUsername;
            _cachedRole = role ?? _cachedRole;

            var dir = Path.GetDirectoryName(TokenFilePath)!;
            Directory.CreateDirectory(dir);

            var data = new TokenData
            {
                Token = token,
                Username = _cachedUsername,
                Role = _cachedRole,
                SavedAt = DateTime.UtcNow
            };
            File.WriteAllText(TokenFilePath, JsonSerializer.Serialize(data));
        }
    }

    public static string? GetToken()
    {
        lock (Lock)
        {
            if (_cachedToken == null)
                LoadFromDisk();
            return _cachedToken;
        }
    }

    public static string? GetUsername()
    {
        lock (Lock)
        {
            if (_cachedToken == null)
                LoadFromDisk();
            return _cachedUsername;
        }
    }

    public static void ClearToken()
    {
        lock (Lock)
        {
            _cachedToken = null;
            _cachedUsername = null;
            _cachedRole = null;
            if (File.Exists(TokenFilePath))
                File.Delete(TokenFilePath);
        }
    }

    private static void LoadFromDisk()
    {
        if (!File.Exists(TokenFilePath))
            return;

        try
        {
            var data = JsonSerializer.Deserialize<TokenData>(File.ReadAllText(TokenFilePath));
            _cachedToken = data?.Token;
            _cachedUsername = data?.Username;
            _cachedRole = data?.Role;
        }
        catch
        {
            _cachedToken = null;
            _cachedUsername = null;
            _cachedRole = null;
        }
    }

    private sealed class TokenData
    {
        public string Token { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? Role { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
