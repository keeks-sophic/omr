namespace BackendV3.Modules.Auth.Dto;

public sealed class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
