using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class NodeConfiguration : IEntityTypeConfiguration<Nodes>
{
    public void Configure(EntityTypeBuilder<Nodes> builder)
    {
        builder.ToTable("nodes");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        builder.Property(n => n.MapId).IsRequired();
        builder.HasIndex(n => n.MapId);

        builder.Property(n => n.Name).HasMaxLength(128).IsRequired();
        builder.Property(n => n.X);
        builder.Property(n => n.Y);

        builder.Property(n => n.Location)
            .HasColumnType("geometry (Point, 0)");

        builder.Property(n => n.Status).HasMaxLength(64).IsRequired();
    }
}
