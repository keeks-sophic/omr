using System;

namespace BackendV2.Api.Model.Auth;

public class UserAccessPolicy
{
    public Guid PolicyId { get; set; }
    public Guid UserId { get; set; }
    public string[]? AllowedRobotIds { get; set; }
    public string[]? AllowedSiteIds { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
