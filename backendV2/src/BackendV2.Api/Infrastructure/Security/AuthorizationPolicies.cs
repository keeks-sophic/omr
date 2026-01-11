using Microsoft.AspNetCore.Authorization;

namespace BackendV2.Api.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
    public const string Planner = "Planner";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, p => p.RequireRole("Admin"));
        options.AddPolicy(Planner, p => p.RequireRole("Planner", "Admin"));
        options.AddPolicy(Operator, p => p.RequireRole("Operator", "Admin"));
        options.AddPolicy(Viewer, p => p.RequireRole("Viewer", "Operator", "Planner", "Admin"));
        options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
    }
}
