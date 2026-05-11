using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AppearanceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "#FF6D00");

            migrationBuilder.AddColumn<string>(
                name: "CustomBackgroundColor",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomForegroundColor",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FontFamily",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FontSize",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 14.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PrimaryColor",          table: "Settings");
            migrationBuilder.DropColumn(name: "CustomBackgroundColor", table: "Settings");
            migrationBuilder.DropColumn(name: "CustomForegroundColor", table: "Settings");
            migrationBuilder.DropColumn(name: "FontFamily",            table: "Settings");
            migrationBuilder.DropColumn(name: "FontSize",              table: "Settings");
        }
    }
}
