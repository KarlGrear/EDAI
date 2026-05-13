using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveKeysAndTestWindowPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AnnounceKeys",
                table: "EventConfigurations");

            migrationBuilder.DropColumn(
                name: "DisplayKeys",
                table: "EventConfigurations");

            migrationBuilder.AddColumn<double>(
                name: "TestWindowHeight",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 680.0);

            migrationBuilder.AddColumn<double?>(
                name: "TestWindowLeft",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double?>(
                name: "TestWindowTop",
                table: "Settings",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TestWindowWidth",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 900.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestWindowHeight",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TestWindowLeft",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TestWindowTop",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TestWindowWidth",
                table: "Settings");

            migrationBuilder.AddColumn<bool>(
                name: "AnnounceKeys",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "DisplayKeys",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
