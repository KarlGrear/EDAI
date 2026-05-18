using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyntaxHighlightingColors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "SyntaxComment",      table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxString",       table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxKeyword",      table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxMethod",       table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxNumber",       table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxPreprocessor", table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxIdentifier",   table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxLineNumber",   table: "Settings", type: "TEXT", nullable: true);
            migrationBuilder.AddColumn<string>(name: "SyntaxBracketMatch", table: "Settings", type: "TEXT", nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SyntaxComment",      table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxString",       table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxKeyword",      table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxMethod",       table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxNumber",       table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxPreprocessor", table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxIdentifier",   table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxLineNumber",   table: "Settings");
            migrationBuilder.DropColumn(name: "SyntaxBracketMatch", table: "Settings");
        }
    }
}
