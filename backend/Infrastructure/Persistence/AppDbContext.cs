using Backend.Model;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Maps> Maps => Set<Maps>();
    public DbSet<Nodes> Nodes => Set<Nodes>();
    public DbSet<Paths> Paths => Set<Paths>();
    public DbSet<Points> Points => Set<Points>();
    public DbSet<Qrs> Qrs => Set<Qrs>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
