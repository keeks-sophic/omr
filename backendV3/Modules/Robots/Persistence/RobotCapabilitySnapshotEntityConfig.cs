using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Robots.Persistence;

public sealed class RobotCapabilitySnapshotEntityConfig : IEntityTypeConfiguration<RobotCapabilitySnapshot>
{
    public void Configure(EntityTypeBuilder<RobotCapabilitySnapshot> builder)
    {
        builder.ToTable("robot_capability_snapshots", RobotsDbSchema.Name);
        builder.HasKey(x => x.SnapshotId);
        builder.Property(x => x.RobotId).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.HasIndex(x => new { x.RobotId, x.ReceivedAt });
    }
}

