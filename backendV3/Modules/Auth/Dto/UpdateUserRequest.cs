namespace BackendV3.Modules.Auth.Dto;

public sealed class UpdateUserRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Password { get; set; }
    public bool? IsDisabled { get; set; }
}
