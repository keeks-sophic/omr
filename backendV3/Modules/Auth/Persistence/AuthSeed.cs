using BackendV3.Infrastructure.Security;
using BackendV3.Modules.Auth.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Modules.Auth.Persistence;

public static class AuthSeed
{
    public static async Task EnsureSeededAsync(BackendV3.Infrastructure.Persistence.AppDbContext db, IServiceProvider services, CancellationToken ct)
    {
        var hasher = services.GetRequiredService<PasswordHasher>();

        await EnsureRoleAsync(db, "Admin", ct);
        await EnsureRoleAsync(db, "Operator", ct);
        await EnsureRoleAsync(db, "Viewer", ct);
        await EnsureRoleAsync(db, "Pending", ct);

        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var admin = new User
        {
            UserId = Guid.NewGuid(),
            Username = "admin",
            DisplayName = "Admin",
            PasswordHash = hasher.Hash("admin123"),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var operatorUser = new User
        {
            UserId = Guid.NewGuid(),
            Username = "operator",
            DisplayName = "Operator",
            PasswordHash = hasher.Hash("operator123"),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var viewer = new User
        {
            UserId = Guid.NewGuid(),
            Username = "viewer",
            DisplayName = "Viewer",
            PasswordHash = hasher.Hash("viewer123"),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        var pending = new User
        {
            UserId = Guid.NewGuid(),
            Username = "pending",
            DisplayName = "Pending",
            PasswordHash = hasher.Hash("pending123"),
            IsDisabled = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Users.AddRange(admin, operatorUser, viewer, pending);
        await db.SaveChangesAsync(ct);

        var roles = await db.Roles.AsNoTracking().ToListAsync(ct);
        var adminRoleId = roles.Single(x => x.Name == "Admin").RoleId;
        var operatorRoleId = roles.Single(x => x.Name == "Operator").RoleId;
        var viewerRoleId = roles.Single(x => x.Name == "Viewer").RoleId;
        var pendingRoleId = roles.Single(x => x.Name == "Pending").RoleId;

        db.UserRoles.AddRange(
            new UserRole { UserId = admin.UserId, RoleId = adminRoleId },
            new UserRole { UserId = operatorUser.UserId, RoleId = operatorRoleId },
            new UserRole { UserId = viewer.UserId, RoleId = viewerRoleId },
            new UserRole { UserId = pending.UserId, RoleId = pendingRoleId }
        );

        await db.SaveChangesAsync(ct);
    }

    private static async Task EnsureRoleAsync(BackendV3.Infrastructure.Persistence.AppDbContext db, string name, CancellationToken ct)
    {
        var exists = await db.Roles.AnyAsync(r => r.Name == name, ct);
        if (exists) return;
        db.Roles.Add(new Role { RoleId = Guid.NewGuid(), Name = name });
        await db.SaveChangesAsync(ct);
    }
}
