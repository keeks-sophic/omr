namespace BackendV3.Modules.Auth.Dto;

public sealed class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}
