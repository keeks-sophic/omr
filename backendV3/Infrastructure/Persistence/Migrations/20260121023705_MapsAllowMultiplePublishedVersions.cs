using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backendV3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapsAllowMultiplePublishedVersions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions",
                column: "MapId",
                filter: "\"Status\" = 'PUBLISHED'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions",
                column: "MapId",
                unique: true,
                filter: "\"Status\" = 'PUBLISHED'");
        }
    }
}
