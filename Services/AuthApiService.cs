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
    private static readonly JsonSerializerOptions Json = JsonFile.CaseInsensitive;

    static AuthApiService()
    {
        Http.DefaultRequestHeaders.Add(VaultGate.Get(Vk.V5), VaultGate.Get(Vk.V6));
    }

    public static async Task<(bool Success, string? Error, bool Locked, LoginResult? Session)> LoginAsync(
        string username,
        string password)
    {
        try
        {
            var hwid = HwidService.GetOrCreate();
            var payload = new AuthLoginRequestDto
            {
                Username = username,
                Password = password,
                Hwid = hwid
            };

            var response = await Http.PostAsJsonAsync(VaultGate.Get(Vk.V2), payload, Json);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadFromJsonAsync<AuthErrorResponseDto>(Json);
                var locked = (int)response.StatusCode == 423;
                return (false, err?.Message ?? "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง", locked, null);
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResult>(Json);
            if (result?.Token == null)
                return (false, "Invalid server response", false, null);

            return (true, null, false, result);
        }
        catch (HttpRequestException)
        {
            return (false, "ไม่สามารถเชื่อมต่อ Server ได้ — ตรวจสอบการเชื่อมต่ออินเทอร์เน็ต", false, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message, false, null);
        }
    }

    public static async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, VaultGate.Get(Vk.V3));
            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await Http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
