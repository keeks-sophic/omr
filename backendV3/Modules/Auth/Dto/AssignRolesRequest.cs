namespace BackendV3.Modules.Auth.Dto;

public sealed class AssignRolesRequest
{
    public string[] Roles { get; set; } = Array.Empty<string>();
}
