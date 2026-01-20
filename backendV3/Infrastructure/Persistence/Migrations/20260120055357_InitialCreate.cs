using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace backendV3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "maps");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "map_nodes",
                schema: "maps",
                columns: table => new
                {
                    NodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    JunctionSpeedLimit = table.Column<double>(type: "double precision", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_nodes", x => x.NodeId);
                });

            migrationBuilder.CreateTable(
                name: "map_paths",
                schema: "maps",
                columns: table => new
                {
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToNodeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<LineString>(type: "geometry(LineString, 0)", nullable: false),
                    LengthMeters = table.Column<double>(type: "double precision", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsMaintenance = table.Column<bool>(type: "boolean", nullable: false),
                    SpeedLimit = table.Column<double>(type: "double precision", nullable: true),
                    IsRestPath = table.Column<bool>(type: "boolean", nullable: false),
                    RestCapacity = table.Column<int>(type: "integer", nullable: true),
                    RestDwellPolicy = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_paths", x => x.PathId);
                });

            migrationBuilder.CreateTable(
                name: "map_points",
                schema: "maps",
                columns: table => new
                {
                    PointId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: false),
                    AttachedNodeId = table.Column<Guid>(type: "uuid", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_map_points", x => x.PointId);
                });

            migrationBuilder.CreateTable(
                name: "map_versions",
                schema: "maps",
                columns: table => new
                {
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
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
                name: "maps",
                schema: "maps",
                columns: table => new
                {
                    MapId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ActiveMapVersionId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maps", x => x.MapId);
                });

            migrationBuilder.CreateTable(
                name: "qr_anchors",
                schema: "maps",
                columns: table => new
                {
                    QrId = table.Column<Guid>(type: "uuid", nullable: false),
                    MapVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PathId = table.Column<Guid>(type: "uuid", nullable: false),
                    QrCode = table.Column<string>(type: "text", nullable: false),
                    DistanceAlongPath = table.Column<double>(type: "double precision", nullable: false),
                    Location = table.Column<Point>(type: "geometry(Point, 0)", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_qr_anchors", x => x.QrId);
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

            migrationBuilder.CreateIndex(
                name: "IX_map_nodes_Location",
                schema: "maps",
                table: "map_nodes",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_map_nodes_MapVersionId",
                schema: "maps",
                table: "map_nodes",
                column: "MapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_map_paths_Location",
                schema: "maps",
                table: "map_paths",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_map_paths_MapVersionId",
                schema: "maps",
                table: "map_paths",
                column: "MapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_map_points_Location",
                schema: "maps",
                table: "map_points",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_map_points_MapVersionId",
                schema: "maps",
                table: "map_points",
                column: "MapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_CreatedAt",
                schema: "maps",
                table: "map_versions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_MapId_Status",
                schema: "maps",
                table: "map_versions",
                columns: new[] { "MapId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_MapId_Version",
                schema: "maps",
                table: "map_versions",
                columns: new[] { "MapId", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_PublishedAt",
                schema: "maps",
                table: "map_versions",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_maps_ActiveMapVersionId",
                schema: "maps",
                table: "maps",
                column: "ActiveMapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_maps_Name",
                schema: "maps",
                table: "maps",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_maps_UpdatedAt",
                schema: "maps",
                table: "maps",
                column: "UpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_qr_anchors_Location",
                schema: "maps",
                table: "qr_anchors",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "IX_qr_anchors_MapVersionId",
                schema: "maps",
                table: "qr_anchors",
                column: "MapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_qr_anchors_PathId",
                schema: "maps",
                table: "qr_anchors",
                column: "PathId");

            migrationBuilder.CreateIndex(
                name: "IX_revoked_tokens_Jti",
                schema: "auth",
                table: "revoked_tokens",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_Name",
                schema: "auth",
                table: "roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_roles_RoleId",
                schema: "auth",
                table: "user_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_users_Username",
                schema: "auth",
                table: "users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "map_nodes",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "map_paths",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "map_points",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "map_versions",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "maps",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "qr_anchors",
                schema: "maps");

            migrationBuilder.DropTable(
                name: "revoked_tokens",
                schema: "auth");

            migrationBuilder.DropTable(
                name: "roles",
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
