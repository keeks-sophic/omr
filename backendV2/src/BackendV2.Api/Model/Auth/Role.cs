using System;

namespace BackendV2.Api.Model.Auth;

public class Role
{
    public Guid RoleId { get; set; }
    public string Name { get; set; } = string.Empty;
}
