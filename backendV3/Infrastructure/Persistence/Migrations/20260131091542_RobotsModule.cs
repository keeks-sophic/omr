using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backendV3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RobotsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "robots");

            migrationBuilder.CreateTable(
                name: "robot_capability_snapshots",
                schema: "robots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_capability_snapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "robot_command_logs",
                schema: "robots",
                columns: table => new
                {
                    CommandId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    CommandType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAckAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_command_logs", x => x.CommandId);
                });

            migrationBuilder.CreateTable(
                name: "robot_identity_snapshots",
                schema: "robots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Vendor = table.Column<string>(type: "text", nullable: true),
                    Model = table.Column<string>(type: "text", nullable: true),
                    FirmwareVersion = table.Column<string>(type: "text", nullable: true),
                    SerialNumber = table.Column<string>(type: "text", nullable: true),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_identity_snapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "robot_settings_reported_snapshots",
                schema: "robots",
                columns: table => new
                {
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_settings_reported_snapshots", x => x.SnapshotId);
                });

            migrationBuilder.CreateTable(
                name: "robots",
                schema: "robots",
                columns: table => new
                {
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    TagsJson = table.Column<string>(type: "text", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robots", x => x.RobotId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_robot_capability_snapshots_RobotId_ReceivedAt",
                schema: "robots",
                table: "robot_capability_snapshots",
                columns: new[] { "RobotId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_robot_command_logs_RobotId_RequestedAt",
                schema: "robots",
                table: "robot_command_logs",
                columns: new[] { "RobotId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_robot_identity_snapshots_RobotId_ReceivedAt",
                schema: "robots",
                table: "robot_identity_snapshots",
                columns: new[] { "RobotId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_robot_settings_reported_snapshots_RobotId_ReceivedAt",
                schema: "robots",
                table: "robot_settings_reported_snapshots",
                columns: new[] { "RobotId", "ReceivedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_robots_RobotId",
                schema: "robots",
                table: "robots",
                column: "RobotId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "robot_capability_snapshots",
                schema: "robots");

            migrationBuilder.DropTable(
                name: "robot_command_logs",
                schema: "robots");

            migrationBuilder.DropTable(
                name: "robot_identity_snapshots",
                schema: "robots");

            migrationBuilder.DropTable(
                name: "robot_settings_reported_snapshots",
                schema: "robots");

            migrationBuilder.DropTable(
                name: "robots",
                schema: "robots");
        }
    }
}
