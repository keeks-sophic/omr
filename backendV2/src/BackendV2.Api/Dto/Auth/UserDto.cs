using System;

namespace BackendV2.Api.Dto.Auth;

public class UserDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string[] Roles { get; set; } = Array.Empty<string>();
    public bool IsDisabled { get; set; }
}
