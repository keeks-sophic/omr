using BackendV3.Modules.Auth.Model;
using BackendV3.Modules.Maps.Model;
using Microsoft.EntityFrameworkCore;

namespace BackendV3.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RevokedToken> RevokedTokens => Set<RevokedToken>();

    public DbSet<Map> Maps => Set<Map>();
    public DbSet<MapVersion> MapVersions => Set<MapVersion>();
    public DbSet<MapNode> MapNodes => Set<MapNode>();
    public DbSet<MapPath> MapPaths => Set<MapPath>();
    public DbSet<MapPoint> MapPoints => Set<MapPoint>();
    public DbSet<QrAnchor> QrAnchors => Set<QrAnchor>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
