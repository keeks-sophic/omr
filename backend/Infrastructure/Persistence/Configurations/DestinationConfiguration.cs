using Backend.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class DestinationConfiguration : IEntityTypeConfiguration<Destinations>
{
    public void Configure(EntityTypeBuilder<Destinations> builder)
    {
        builder.ToTable("destinations");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.RobotId).IsRequired();
        builder.Property(d => d.MapId).IsRequired();
        builder.Property(d => d.X).IsRequired();
        builder.Property(d => d.Y).IsRequired();
        builder.Property(d => d.Location)
            .HasColumnType("geometry (Point, 0)");

        builder.HasIndex(d => d.RobotId);
        builder.HasIndex(d => d.MapId);

        builder.HasOne<Robot>().WithMany().HasForeignKey(d => d.RobotId).OnDelete(DeleteBehavior.Cascade);
    }
}

