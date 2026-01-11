using BackendV2.Api.Model.Auth;
using BackendV2.Api.Model.Core;
using BackendV2.Api.Model.Map;
using BackendV2.Api.Model.Task;
using BackendV2.Api.Model.Traffic;
using BackendV2.Api.Model.Ops;
using BackendV2.Api.Model.Sim;
using BackendV2.Api.Model.Replay;
using Microsoft.EntityFrameworkCore;

namespace BackendV2.Api.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserAccessPolicy> UserAccessPolicies => Set<UserAccessPolicy>();
    public DbSet<BackendV2.Api.Model.Auth.RevokedToken> RevokedTokens => Set<BackendV2.Api.Model.Auth.RevokedToken>();

    public DbSet<Robot> Robots => Set<Robot>();
    public DbSet<RobotSession> RobotSessions => Set<RobotSession>();

    public DbSet<MapVersion> MapVersions => Set<MapVersion>();
    public DbSet<MapNode> Nodes => Set<MapNode>();
    public DbSet<MapPath> Paths => Set<MapPath>();
    public DbSet<MapPoint> Points => Set<MapPoint>();
    public DbSet<QrAnchor> QrAnchors => Set<QrAnchor>();

    public DbSet<BackendV2.Api.Model.Task.Task> Tasks => Set<BackendV2.Api.Model.Task.Task>();
    public DbSet<BackendV2.Api.Model.Task.Route> Routes => Set<BackendV2.Api.Model.Task.Route>();
    public DbSet<Mission> Missions => Set<Mission>();
    public DbSet<TeachSession> TeachSessions => Set<TeachSession>();
    public DbSet<TaskEvent> TaskEvents => Set<TaskEvent>();

    public DbSet<TrafficHold> TrafficHolds => Set<TrafficHold>();

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    public DbSet<BackendV2.Api.Model.Ops.CommandOutbox> CommandOutbox => Set<BackendV2.Api.Model.Ops.CommandOutbox>();

    public DbSet<SimSession> SimSessions => Set<SimSession>();

    public DbSet<ReplaySession> ReplaySessions => Set<ReplaySession>();
    public DbSet<BackendV2.Api.Model.Replay.RobotEvent> RobotEvents => Set<BackendV2.Api.Model.Replay.RobotEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");

        modelBuilder.Entity<User>().ToTable("users", "auth").HasKey(x => x.UserId);
        modelBuilder.Entity<Role>().ToTable("roles", "auth").HasKey(x => x.RoleId);
        modelBuilder.Entity<UserRole>().ToTable("user_roles", "auth").HasKey(x => new { x.UserId, x.RoleId });
        modelBuilder.Entity<UserAccessPolicy>().ToTable("user_access_policies", "auth").HasKey(x => x.PolicyId);
        modelBuilder.Entity<BackendV2.Api.Model.Auth.RevokedToken>().ToTable("revoked_tokens", "auth").HasKey(x => x.RevocationId);

        modelBuilder.Entity<Robot>().ToTable("robots", "core").HasKey(x => x.RobotId);
        modelBuilder.Entity<Robot>().Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        modelBuilder.Entity<RobotSession>().ToTable("robot_sessions", "core").HasKey(x => x.RobotId);

        modelBuilder.Entity<MapVersion>().ToTable("map_versions", "map").HasKey(x => x.MapVersionId);
        modelBuilder.Entity<MapNode>().ToTable("nodes", "map").HasKey(x => x.NodeId);
        modelBuilder.Entity<MapNode>().Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        modelBuilder.Entity<MapPath>().ToTable("paths", "map").HasKey(x => x.PathId);
        modelBuilder.Entity<MapPath>().Property(x => x.Location).HasColumnType("geometry(LineString, 0)");
        modelBuilder.Entity<MapPoint>().ToTable("points", "map").HasKey(x => x.PointId);
        modelBuilder.Entity<MapPoint>().Property(x => x.Location).HasColumnType("geometry(Point, 0)");
        modelBuilder.Entity<QrAnchor>().ToTable("qr_anchors", "map").HasKey(x => x.QrId);
        modelBuilder.Entity<QrAnchor>().Property(x => x.Location).HasColumnType("geometry(Point, 0)");

        modelBuilder.Entity<BackendV2.Api.Model.Task.Task>().ToTable("tasks", "task").HasKey(x => x.TaskId);
        modelBuilder.Entity<BackendV2.Api.Model.Task.Route>().ToTable("routes", "task").HasKey(x => x.RouteId);
        modelBuilder.Entity<BackendV2.Api.Model.Task.Route>().Property(x => x.Start).HasColumnType("geometry(Point, 0)");
        modelBuilder.Entity<BackendV2.Api.Model.Task.Route>().Property(x => x.Goal).HasColumnType("geometry(Point, 0)");
        modelBuilder.Entity<Mission>().ToTable("missions", "task").HasKey(x => x.MissionId);
        modelBuilder.Entity<TeachSession>().ToTable("teach_sessions", "task").HasKey(x => x.TeachSessionId);
        modelBuilder.Entity<TaskEvent>().ToTable("task_events", "task").HasKey(x => x.TaskEventId);

        modelBuilder.Entity<TrafficHold>().ToTable("holds", "traffic").HasKey(x => x.HoldId);

        modelBuilder.Entity<AuditEvent>().ToTable("audit_events", "ops").HasKey(x => x.AuditEventId);
        modelBuilder.Entity<BackendV2.Api.Model.Ops.CommandOutbox>().ToTable("command_outbox", "ops").HasKey(x => x.OutboxId);

        modelBuilder.Entity<SimSession>().ToTable("sim_sessions", "sim").HasKey(x => x.SimSessionId);

        modelBuilder.Entity<ReplaySession>().ToTable("replay_sessions", "replay").HasKey(x => x.ReplaySessionId);
        modelBuilder.Entity<BackendV2.Api.Model.Replay.RobotEvent>().ToTable("robot_events", "replay").HasKey(x => x.EventId);
        modelBuilder.Entity<BackendV2.Api.Model.Replay.RobotEvent>().Property(x => x.Payload).HasColumnType("jsonb");
    }
}
