using Microsoft.EntityFrameworkCore;
using backend.Models;
using NetTopologySuite.Geometries;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<Map> Maps => Set<Map>();
    public DbSet<Node> Nodes => Set<Node>();
    public DbSet<Path> Paths => Set<Path>();
    public DbSet<MapPoint> MapPoints => Set<MapPoint>();
    public DbSet<Qr> Qrs => Set<Qr>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Robot>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
            b.Property(x => x.Ip).HasMaxLength(64);
            b.Property(x => x.Geom).HasColumnType("geometry(Point,0)");
            b.HasIndex(x => x.Ip);
        });

        modelBuilder.Entity<Map>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
        });

        modelBuilder.Entity<Node>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
            b.Property(x => x.Status).HasMaxLength(32);
            b.Property(x => x.Location).HasColumnType("geometry(Point,0)");
            b.HasOne(x => x.Map).WithMany(m => m.Nodes).HasForeignKey(x => x.MapId);
        });

        modelBuilder.Entity<Path>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Status).HasMaxLength(32);
            b.Property(x => x.Location).HasColumnType("geometry(LineString,0)");
            b.HasOne(x => x.Map).WithMany(m => m.Paths).HasForeignKey(x => x.MapId);
            b.HasOne(x => x.StartNode).WithMany().HasForeignKey(x => x.StartNodeId);
            b.HasOne(x => x.EndNode).WithMany().HasForeignKey(x => x.EndNodeId);
        });

        modelBuilder.Entity<MapPoint>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(128);
            b.Property(x => x.Type).HasMaxLength(32);
            b.HasOne(x => x.Map).WithMany(m => m.MapPoints).HasForeignKey(x => x.MapId);
            b.HasOne(x => x.Path).WithMany().HasForeignKey(x => x.PathId);
        });

        modelBuilder.Entity<Qr>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Data).HasMaxLength(512);
            b.Property(x => x.Location).HasColumnType("geometry(Point,0)");
            b.HasOne(x => x.Map).WithMany(m => m.Qrs).HasForeignKey(x => x.MapId);
            b.HasOne(x => x.Path).WithMany().HasForeignKey(x => x.PathId);
        });
    }
}
