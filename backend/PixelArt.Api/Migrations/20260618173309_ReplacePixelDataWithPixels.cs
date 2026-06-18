using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PixelArt.Api.Migrations
{
    /// <inheritdoc />
    public partial class ReplacePixelDataWithPixels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PixelData",
                table: "Drawings",
                newName: "Pixels");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Pixels",
                table: "Drawings",
                newName: "PixelData");
        }
    }
}
