using System;

namespace BackendV2.Api.Dto.Auth;

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = default!;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshExpiresAt { get; set; }
}
