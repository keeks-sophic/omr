namespace BackendV3.Infrastructure.Security;

public class JwtOptions
{
    public string Issuer { get; set; } = "newsky-backend";
    public string Audience { get; set; } = "newsky-frontend";
    public string Key { get; set; } = "dev-secret-change-please-use-env-BACKENDV3_JWTKEY-0123456789abcdef0123456789abcdef";
    public int ExpiryMinutes { get; set; } = 60;
}
