using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolirante_Open_Minded.Migrations
{
    /// <inheritdoc />
    public partial class ModifyShelfDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
    name: "ShelfCapacity",
    table: "Merchandise", // make sure this matches your actual table name
    type: "int",
    nullable: false,
    defaultValue: 25,
    oldClrType: typeof(int),
    oldType: "int",
    oldDefaultValue: 25);

            migrationBuilder.AlterColumn<int>(
                name: "CurrentShelf",
                table: "Merchandise",
                type: "int",
                nullable: false,
                defaultValue: 20,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);



        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
