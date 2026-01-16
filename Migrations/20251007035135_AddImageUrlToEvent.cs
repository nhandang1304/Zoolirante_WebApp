using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Zoolirante_Open_Minded.Migrations
{
	public partial class AddImageUrlToEvent : Migration
	{
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.AddColumn<string>(
				name: "ImageUrl",
				table: "Events",
				type: "nvarchar(255)",
				maxLength: 255,
				nullable: true);
		}

		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropColumn(
				name: "ImageUrl",
				table: "Events");
		}
	}
}
