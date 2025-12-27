using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Robots");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_Ip",
                table: "Robots",
                column: "Ip");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Robots_Ip",
                table: "Robots");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "Robots",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);
        }
    }
}
