using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BackendV2.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "traffic");

            migrationBuilder.EnsureSchema(
                name: "map");

            migrationBuilder.EnsureSchema(
                name: "task");

            migrationBuilder.EnsureSchema(
                name: "replay");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "sim");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "audit_events",
                schema: "ops",
                columns: table => new
                {
                    AuditEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorRoles = table.Column<string>(type: "text", nullable: true),
                    Action = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: true),
                    TargetId = table.Column<string>(type: "text", nullable: true),
                    Outcome = table.Column<string>(type: "text", nullable: false),
                    DetailsJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_events", x => x.AuditEventId);
                });

            migrationBuilder.CreateTable(
                name: "command_outbox",
                schema: "ops",
                columns: table => new
                {
                    OutboxId = table.Column<Guid>(type: "uuid", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Subject = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAttempt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_command_outbox", x => x.OutboxId);
                });

            migrationBuilder.CreateTable(
                name: "holds",
                schema: "traffic",
                columns: table => new
                {
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    NodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    PathId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_holds", x => x.HoldId);
                });

            migrationBuilder.CreateTable(
                name: "map_versions",
                schema: "map",
                columns: table => new
                {
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ChangeSummary = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_versions", x => x.MapVersionId);
                });

            migrationBuilder.CreateTable(
                name: "missions",
                schema: "task",
                columns: table => new
                {
                    MissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    StepsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_missions", x => x.MissionId);
                });

            migrationBuilder.CreateTable(
                name: "nodes",
                schema: "map",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_nodes", x => x.NodeId);
                });

            migrationBuilder.CreateTable(
                name: "paths",
                schema: "map",
                columns: table => new
                {
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Location = table.Column<LineString>(type: "geometry(LineString, 0)", nullable: false),
                    TwoWay = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    SpeedLimit = table.Column<double>(type: "double precision", nullable: true),
                    IsRestPath = table.Column<bool>(type: "boolean", nullable: false),
                    RestCapacity = table.Column<int>(type: "integer", nullable: true),
                    RestDwellPolicyJson = table.Column<string>(type: "text", nullable: true),
                    MinFollowingDistanceMeters = table.Column<double>(type: "double precision", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_paths", x => x.PathId);
                });

            migrationBuilder.CreateTable(
                name: "points",
                schema: "map",
                columns: table => new
                {
                    PointId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    AttachedNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_points", x => x.PointId);
                });

            migrationBuilder.CreateTable(
                name: "qr_anchors",
                schema: "map",
                columns: table => new
                {
                    QrId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCode = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    DistanceAlongPath = table.Column<double>(type: "double precision", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_anchors", x => x.QrId);
                });

            migrationBuilder.CreateTable(
                name: "replay_sessions",
                schema: "replay",
                columns: table => new
                {
                    ReplaySessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    FromTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ToTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PlaybackSpeed = table.Column<double>(type: "double precision", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_replay_sessions", x => x.ReplaySessionId);
                });

            migrationBuilder.CreateTable(
                name: "revoked_tokens",
                schema: "auth",
                columns: table => new
                {
                    RevocationId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Jti = table.Column<string>(type: "text", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_revoked_tokens", x => x.RevocationId);
                });

            migrationBuilder.CreateTable(
                name: "robot_events",
                schema: "replay",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_events", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "robot_sessions",
                schema: "core",
                columns: table => new
                {
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Connected = table.Column<bool>(type: "boolean", nullable: false),
                    LastSeen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RuntimeMode = table.Column<string>(type: "text", nullable: false),
                    SoftwareVersion = table.Column<string>(type: "text", nullable: true),
                    CapabilitiesJson = table.Column<string>(type: "text", nullable: false),
                    FeatureFlagsJson = table.Column<string>(type: "text", nullable: false),
                    MotionLimitsJson = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robot_sessions", x => x.RobotId);
                });

            migrationBuilder.CreateTable(
                name: "robots",
                schema: "core",
                columns: table => new
                {
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Ip = table.Column<string>(type: "text", nullable: true),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: true),
                    X = table.Column<double>(type: "double precision", nullable: true),
                    Y = table.Column<double>(type: "double precision", nullable: true),
                    State = table.Column<string>(type: "text", nullable: true),
                    Battery = table.Column<double>(type: "double precision", nullable: true),
                    Connected = table.Column<bool>(type: "boolean", nullable: false),
                    LastActive = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_robots", x => x.RobotId);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "auth",
                columns: table => new
                {
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.RoleId);
                });

            migrationBuilder.CreateTable(
                name: "routes",
                schema: "task",
                columns: table => new
                {
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Start = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    Goal = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    SegmentsJson = table.Column<string>(type: "text", nullable: false),
                    EstimatedStartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EstimatedArrivalTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.RouteId);
                });

            migrationBuilder.CreateTable(
                name: "sim_sessions",
                schema: "sim",
                columns: table => new
                {
                    SimSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    SpeedMultiplier = table.Column<double>(type: "double precision", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfigJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sim_sessions", x => x.SimSessionId);
                });

            migrationBuilder.CreateTable(
                name: "task_events",
                schema: "task",
                columns: table => new
                {
                    TaskEventId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Detail = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_task_events", x => x.TaskEventId);
                });

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "task",
                columns: table => new
                {
                    TaskId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    AssignmentMode = table.Column<string>(type: "text", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: true),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaskType = table.Column<string>(type: "text", nullable: false),
                    ParametersJson = table.Column<string>(type: "text", nullable: false),
                    MissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentRouteId = table.Column<Guid>(type: "uuid", nullable: true),
                    Eta = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tasks", x => x.TaskId);
                });

            migrationBuilder.CreateTable(
                name: "teach_sessions",
                schema: "task",
                columns: table => new
                {
                    TeachSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RobotId = table.Column<string>(type: "text", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StoppedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CapturedStepsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_teach_sessions", x => x.TeachSessionId);
                });

            migrationBuilder.CreateTable(
                name: "user_access_policies",
                schema: "auth",
                columns: table => new
                {
                    PolicyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AllowedRobotIds = table.Column<string[]>(type: "text[]", nullable: true),
                    AllowedSiteIds = table.Column<string[]>(type: "text[]", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_access_policies", x => x.PolicyId);
                });

            migrationBuilder.CreateTable(
                name: "user_roles",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_roles", x => new { x.UserId, x.RoleId });
                });

            migrationBuilder.CreateTable(
                name: "users",
                schema: "auth",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    IsDisabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.UserId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_events",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "command_outbox",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "holds",
                schema: "traffic");

            migrationBuilder.DropTable(
                name: "map_versions",
                schema: "map");

            migrationBuilder.DropTable(
                name: "missions",
                schema: "task");

            migrationBuilder.DropTable(
                name: "nodes",
                schema: "map");

            migrationBuilder.DropTable(
                name: "paths",
                schema: "map");

            migrationBuilder.DropTable(
                name: "points",
                schema: "map");

            migrationBuilder.DropTable(
                name: "qr_anchors",
                schema: "map");

            migrationBuilder.DropTable(
                name: "replay_sessions",
                schema: "replay");

            migrationBuilder.DropTable(
                name: "revoked_tokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "robot_events",
                schema: "replay");

            migrationBuilder.DropTable(
                name: "robot_sessions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "robots",
                schema: "core");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "routes",
                schema: "task");

            migrationBuilder.DropTable(
                name: "sim_sessions",
                schema: "sim");

            migrationBuilder.DropTable(
                name: "task_events",
                schema: "task");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "task");

            migrationBuilder.DropTable(
                name: "teach_sessions",
                schema: "task");

            migrationBuilder.DropTable(
                name: "user_access_policies",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "user_roles",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "users",
                schema: "auth");
        }
    }
}
