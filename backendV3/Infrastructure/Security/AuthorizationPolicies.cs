using Microsoft.AspNetCore.Authorization;

namespace BackendV3.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";
    public const string Pending = "Pending";
    public const string AnyAuthenticated = "AnyAuthenticated";

    public static void AddPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(Admin, p => p.RequireRole("Admin"));
        options.AddPolicy(Operator, p => p.RequireRole("Operator", "Admin"));
        options.AddPolicy(Viewer, p => p.RequireRole("Viewer", "Operator", "Admin"));
        options.AddPolicy(AnyAuthenticated, p => p.RequireAuthenticatedUser());

        var nonPending = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .RequireRole("Viewer", "Operator", "Admin")
            .Build();

        options.DefaultPolicy = nonPending;
        options.FallbackPolicy = nonPending;
    }
}
