using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class RobotConfiguration : IEntityTypeConfiguration<Robot>
{
    public void Configure(EntityTypeBuilder<Robot> builder)
    {
        builder.ToTable("robots");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id)
            .ValueGeneratedOnAdd();

        builder.Property(r => r.Ip)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Name)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(r => r.X).IsRequired(false);
        builder.Property(r => r.Y).IsRequired(false);

        builder.Property(r => r.Location)
            .HasColumnType("geometry (Point, 0)");

        builder.Property(r => r.State)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(r => r.Battery);
        builder.Property(r => r.Connected);
        builder.Property(r => r.LastActive);

        builder.Property(r => r.MapId)
            .IsRequired(false);

        builder.HasIndex(r => r.MapId);
    }
}
