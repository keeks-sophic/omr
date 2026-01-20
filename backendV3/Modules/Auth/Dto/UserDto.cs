namespace BackendV3.Modules.Auth.Dto;

public sealed class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsDisabled { get; set; }
}
