using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEdgeTts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EdgeTtsLanguage",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EdgeTtsVoice",
                table: "Settings",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TtsProvider",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "SAPI");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EdgeTtsLanguage",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "EdgeTtsVoice",
                table: "Settings");

            migrationBuilder.DropColumn(
                name: "TtsProvider",
                table: "Settings");
        }
    }
}
