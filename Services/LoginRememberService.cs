using System.Security.Cryptography;

namespace TEMO.AI;

internal static class LoginRememberService
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "TEMO.AI", "login_remember.json");

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    private sealed class Persisted
    {
        public bool RememberUsername { get; set; }
        public string? Username { get; set; }
        public string? PasswordProtectedBase64 { get; set; }
    }

    public static (string? Username, string? Password, bool RememberChecked) Load()
    {
        try
        {
            var p = JsonFile.Read<Persisted>(FilePath, Json);
            if (p == null || !p.RememberUsername || string.IsNullOrWhiteSpace(p.Username))
                return (null, null, false);

            string? password = null;
            if (!string.IsNullOrEmpty(p.PasswordProtectedBase64))
            {
                try
                {
                    var blob = Convert.FromBase64String(p.PasswordProtectedBase64);
                    var plain = ProtectedData.Unprotect(blob, null, DataProtectionScope.CurrentUser);
                    password = Encoding.UTF8.GetString(plain);
                }
                catch { password = null; }
            }

            return (p.Username.Trim(), password, true);
        }
        catch { return (null, null, false); }
    }

    public static void Save(string? username, string? plainPassword, bool rememberCredentials)
    {
        try
        {
            if (!rememberCredentials)
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
                return;
            }

            string? pwdB64 = null;
            if (!string.IsNullOrEmpty(plainPassword))
            {
                var bytes = ProtectedData.Protect(
                    Encoding.UTF8.GetBytes(plainPassword), null, DataProtectionScope.CurrentUser);
                pwdB64 = Convert.ToBase64String(bytes);
            }

            var p = new Persisted
            {
                RememberUsername = true,
                Username = username?.Trim() ?? "",
                PasswordProtectedBase64 = pwdB64,
            };
            JsonFile.Write(FilePath, p, Json);
        }
        catch { }
    }
}
