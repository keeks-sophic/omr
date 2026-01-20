namespace BackendV3.Modules.Auth.Model;

public class RevokedToken
{
    public Guid RevocationId { get; set; }
    public Guid? UserId { get; set; }
    public string Jti { get; set; } = string.Empty;
    public DateTimeOffset RevokedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
