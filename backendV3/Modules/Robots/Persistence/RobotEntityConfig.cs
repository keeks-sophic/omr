using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Robots.Persistence;

public sealed class RobotEntityConfig : IEntityTypeConfiguration<Robot>
{
    public void Configure(EntityTypeBuilder<Robot> builder)
    {
        builder.ToTable("robots", RobotsDbSchema.Name);
        builder.HasKey(x => x.RobotId);
        builder.Property(x => x.RobotId).IsRequired();
        builder.Property(x => x.DisplayName).IsRequired();
        builder.Property(x => x.IsEnabled).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.HasIndex(x => x.RobotId).IsUnique();
    }
}

