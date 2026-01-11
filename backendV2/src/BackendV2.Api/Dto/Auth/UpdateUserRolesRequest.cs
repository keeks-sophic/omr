namespace BackendV2.Api.Dto.Auth;

public class UpdateUserRolesRequest
{
    public string[] Roles { get; set; } = System.Array.Empty<string>();
}
