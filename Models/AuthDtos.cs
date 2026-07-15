using System.Text.Json.Serialization;

namespace TEMO.AI;

public sealed class AuthLoginRequestDto
{
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("password")]
    public string Password { get; set; } = string.Empty;

    [JsonPropertyName("hwid")]
    public string Hwid { get; set; } = string.Empty;
}

public sealed class AuthLoginResponseDto
{
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("username")]
    public string? Username { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class AuthErrorResponseDto
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
