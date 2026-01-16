using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolirante_Open_Minded.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfFieldsToMerchandise : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrentShelf",
                table: "Merchandise",
                type: "int",
                nullable: false,
                defaultValue: 25);

            migrationBuilder.AddColumn<int>(
                name: "ShelfCapacity",
                table: "Merchandise",
                type: "int",
                nullable: false,
                defaultValue: 20);



        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDetail");

            migrationBuilder.DropColumn(
                name: "CurrentShelf",
                table: "Merchandise");

            migrationBuilder.DropColumn(
                name: "ShelfCapacity",
                table: "Merchandise");
        }
    }
}
