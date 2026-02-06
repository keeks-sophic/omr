using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backendV3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapsVersionedEntityKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_qr_anchors",
                schema: "maps",
                table: "qr_anchors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_points",
                schema: "maps",
                table: "map_points");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_paths",
                schema: "maps",
                table: "map_paths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_nodes",
                schema: "maps",
                table: "map_nodes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_qr_anchors",
                schema: "maps",
                table: "qr_anchors",
                columns: new[] { "MapVersionId", "QrId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_points",
                schema: "maps",
                table: "map_points",
                columns: new[] { "MapVersionId", "PointId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_paths",
                schema: "maps",
                table: "map_paths",
                columns: new[] { "MapVersionId", "PathId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_nodes",
                schema: "maps",
                table: "map_nodes",
                columns: new[] { "MapVersionId", "NodeId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_qr_anchors",
                schema: "maps",
                table: "qr_anchors");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_points",
                schema: "maps",
                table: "map_points");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_paths",
                schema: "maps",
                table: "map_paths");

            migrationBuilder.DropPrimaryKey(
                name: "PK_map_nodes",
                schema: "maps",
                table: "map_nodes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_qr_anchors",
                schema: "maps",
                table: "qr_anchors",
                column: "QrId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_points",
                schema: "maps",
                table: "map_points",
                column: "PointId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_paths",
                schema: "maps",
                table: "map_paths",
                column: "PathId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_map_nodes",
                schema: "maps",
                table: "map_nodes",
                column: "NodeId");
        }
    }
}
