using BackendV3.Modules.Robots.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BackendV3.Modules.Robots.Persistence;

public sealed class RobotCommandLogEntityConfig : IEntityTypeConfiguration<RobotCommandLog>
{
    public void Configure(EntityTypeBuilder<RobotCommandLog> builder)
    {
        builder.ToTable("robot_command_logs", RobotsDbSchema.Name);
        builder.HasKey(x => x.CommandId);
        builder.Property(x => x.RobotId).IsRequired();
        builder.Property(x => x.CommandType).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RequestedAt).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.HasIndex(x => new { x.RobotId, x.RequestedAt });
    }
}

