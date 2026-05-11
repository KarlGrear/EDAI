using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsMaximized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsMaximized",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsMaximized",
                table: "Settings");
        }
    }
}
