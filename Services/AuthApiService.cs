using System.Net.Http.Json;

namespace TEMO.AI;

public sealed class LoginResult
{
    public string? Token { get; set; }
    public string? Role { get; set; }
    public string? Username { get; set; }
}

public static class AuthApiService
{
    private static readonly HttpClient Http = new();
    private const string BaseUrl = "https://temo-backend.onrender.com/api";

    private static readonly JsonSerializerOptions Json = JsonFile.CaseInsensitive;

    public static async Task<(bool Success, string? Error, bool Locked)> LoginAsync(string username, string password)
    {
        try
        {
            var hwid = HwidService.GetOrCreate();
            var response = await Http.PostAsJsonAsync($"{BaseUrl}/auth/login", new { username, password, hwid });
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<ErrorResponse>(Json);
                var locked = (int)response.StatusCode == 423;
                return (false, err?.Message ?? "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง", locked);
            }
            var result = await response.Content.ReadFromJsonAsync<LoginResult>(Json);
            if (result?.Token == null) return (false, "Invalid server response", false);
            return (true, null, false);
        }
        catch (HttpRequestException)
        {
            return (false, "ไม่สามารถเชื่อมต่อ Server ได้ — ตรวจสอบการเชื่อมต่ออินเทอร์เน็ต", false);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, false);
        }
    }

    private sealed class ErrorResponse { public string? Message { get; set; } }
}
