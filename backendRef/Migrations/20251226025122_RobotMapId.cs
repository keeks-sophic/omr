using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RobotMapId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MapId",
                table: "Robots",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_MapId",
                table: "Robots",
                column: "MapId");

            migrationBuilder.AddForeignKey(
                name: "FK_Robots_Maps_MapId",
                table: "Robots",
                column: "MapId",
                principalTable: "Maps",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Robots_Maps_MapId",
                table: "Robots");

            migrationBuilder.DropIndex(
                name: "IX_Robots_MapId",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "MapId",
                table: "Robots");
        }
    }
}
