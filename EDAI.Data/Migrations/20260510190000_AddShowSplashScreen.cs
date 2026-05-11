using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShowSplashScreen : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ShowSplashScreen",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShowSplashScreen",
                table: "Settings");
        }
    }
}
