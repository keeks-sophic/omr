using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Robots.Persistence;

public sealed class RobotSettingsReportedSnapshotEntityConfig : IEntityTypeConfiguration<RobotSettingsReportedSnapshot>
{
    public void Configure(EntityTypeBuilder<RobotSettingsReportedSnapshot> builder)
    {
        builder.ToTable("robot_settings_reported_snapshots", RobotsDbSchema.Name);
        builder.HasKey(x => x.SnapshotId);
        builder.Property(x => x.RobotId).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ReceivedAt).IsRequired();
        builder.HasIndex(x => new { x.RobotId, x.ReceivedAt });
    }
}

