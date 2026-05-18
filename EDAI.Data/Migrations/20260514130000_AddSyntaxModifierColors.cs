using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyntaxModifierColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "SyntaxTypeKeyword",    table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxContextKeyword", table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxModifier",       table: "Settings", type: "TEXT", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SyntaxTypeKeyword",    table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxContextKeyword", table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxModifier",       table: "Settings");
        }
    }
}
