using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class MapPointConfiguration : IEntityTypeConfiguration<Points>
{
    public void Configure(EntityTypeBuilder<Points> builder)
    {
        builder.ToTable("points");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedOnAdd();

        builder.Property(p => p.MapId).IsRequired();
        builder.HasIndex(p => p.MapId);
        builder.Property(p => p.PathId).IsRequired();
        builder.HasIndex(p => p.PathId);

        builder.Property(p => p.Offset);
        builder.Property(p => p.Type).HasMaxLength(64).IsRequired();
        builder.Property(p => p.Name).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Location)
            .HasColumnType("geometry (Point, 0)");

        builder.HasOne<Paths>().WithMany().HasForeignKey(p => p.PathId).OnDelete(DeleteBehavior.Cascade);
    }
}
