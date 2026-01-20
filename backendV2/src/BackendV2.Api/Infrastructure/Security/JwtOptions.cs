namespace BackendV2.Api.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "newsky-backend";
    public string Audience { get; set; } = "newsky-frontend";
    public string Key { get; set; } = "dev-secret-change-please-use-env-BACKENDV2_JWTKEY-0123456789abcdef0123456789abcdef";
    public int ExpiryMinutes { get; set; } = 60;
    public string RefreshAudience { get; set; } = "newsky-refresh";
    public int RefreshExpiryMinutes { get; set; } = 1440;
}
