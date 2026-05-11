using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddToolbarButtonColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ToolbarBackground",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ToolbarForeground",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ButtonForeground",
                table: "Settings",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ToolbarBackground",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ToolbarForeground",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "ButtonForeground",
                table: "Settings");
        }
    }
}
