using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class PathConfiguration : IEntityTypeConfiguration<Paths>
{
    public void Configure(EntityTypeBuilder<Paths> builder)
    {
        builder.ToTable("paths");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.MapId).IsRequired();
        builder.HasIndex(p => p.MapId);
        builder.Property(p => p.StartNodeId).IsRequired();
        builder.Property(p => p.EndNodeId).IsRequired();
        builder.HasIndex(p => p.StartNodeId);
        builder.HasIndex(p => p.EndNodeId);

        builder.Property(p => p.Location)
            .HasColumnType("geometry (LineString, 0)");

        builder.Property(p => p.TwoWay).IsRequired();
        builder.Property(p => p.Length);
        builder.Property(p => p.Status).HasMaxLength(64).IsRequired();

        builder.HasOne<Nodes>().WithMany().HasForeignKey(p => p.StartNodeId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Nodes>().WithMany().HasForeignKey(p => p.EndNodeId).OnDelete(DeleteBehavior.Restrict);
    }
}
