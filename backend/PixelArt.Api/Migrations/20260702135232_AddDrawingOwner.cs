using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelArt.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddDrawingOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "Drawings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Drawings_UserId",
                table: "Drawings",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Drawings_Users_UserId",
                table: "Drawings",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Drawings_Users_UserId",
                table: "Drawings");

            migrationBuilder.DropIndex(
                name: "IX_Drawings_UserId",
                table: "Drawings");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Drawings");
        }
    }
}
