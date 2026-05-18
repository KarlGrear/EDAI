using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptingPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ScriptingAllowFileSystem",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScriptingAllowNetwork",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScriptingAllowProcessExecution",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ScriptingAllowReflection",
                table: "Settings",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ScriptingAllowFileSystem",       table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptingAllowNetwork",          table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptingAllowProcessExecution", table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptingAllowReflection",       table: "Settings");
        }
    }
}
