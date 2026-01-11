using System;
namespace BackendV2.Api.Dto.Auth;

public class RefreshResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset RefreshExpiresAt { get; set; }
}
