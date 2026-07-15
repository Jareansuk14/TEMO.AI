namespace TEMO.AI;

public static class AuthSession
{
    public static async Task<bool> TryRestoreSessionAsync()
    {
        var token = TokenManager.GetToken();
        if (string.IsNullOrEmpty(token))
            return false;

        if (await AuthApiService.ValidateTokenAsync(token))
            return true;

        TokenManager.ClearToken();
        return false;
    }
}
