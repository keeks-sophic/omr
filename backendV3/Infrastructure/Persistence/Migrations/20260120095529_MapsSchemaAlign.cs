using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backendV3.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MapsSchemaAlign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maps_UpdatedAt",
                schema: "maps",
                table: "maps");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                schema: "maps",
                table: "maps");

            migrationBuilder.RenameColumn(
                name: "ActiveMapVersionId",
                schema: "maps",
                table: "maps",
                newName: "ActivePublishedMapVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_maps_ActiveMapVersionId",
                schema: "maps",
                table: "maps",
                newName: "IX_maps_ActivePublishedMapVersionId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ArchivedAt",
                schema: "maps",
                table: "maps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DerivedFromMapVersionId",
                schema: "maps",
                table: "map_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Label",
                schema: "maps",
                table: "map_versions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PublishedBy",
                schema: "maps",
                table: "map_versions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_maps_ArchivedAt",
                schema: "maps",
                table: "maps",
                column: "ArchivedAt");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_DerivedFromMapVersionId",
                schema: "maps",
                table: "map_versions",
                column: "DerivedFromMapVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions",
                column: "MapId",
                unique: true,
                filter: "\"Status\" = 'PUBLISHED'");

            migrationBuilder.CreateIndex(
                name: "IX_map_versions_PublishedBy",
                schema: "maps",
                table: "map_versions",
                column: "PublishedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_maps_ArchivedAt",
                schema: "maps",
                table: "maps");

            migrationBuilder.DropIndex(
                name: "IX_map_versions_DerivedFromMapVersionId",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.DropIndex(
                name: "IX_map_versions_MapId",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.DropIndex(
                name: "IX_map_versions_PublishedBy",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.DropColumn(
                name: "ArchivedAt",
                schema: "maps",
                table: "maps");

            migrationBuilder.DropColumn(
                name: "DerivedFromMapVersionId",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.DropColumn(
                name: "Label",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.DropColumn(
                name: "PublishedBy",
                schema: "maps",
                table: "map_versions");

            migrationBuilder.RenameColumn(
                name: "ActivePublishedMapVersionId",
                schema: "maps",
                table: "maps",
                newName: "ActiveMapVersionId");

            migrationBuilder.RenameIndex(
                name: "IX_maps_ActivePublishedMapVersionId",
                schema: "maps",
                table: "maps",
                newName: "IX_maps_ActiveMapVersionId");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                schema: "maps",
                table: "maps",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_maps_UpdatedAt",
                schema: "maps",
                table: "maps",
                column: "UpdatedAt");
        }
    }
}
