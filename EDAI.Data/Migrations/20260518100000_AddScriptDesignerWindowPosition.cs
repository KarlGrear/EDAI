using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptDesignerWindowPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double?>(name: "ScriptDesignerLeft",   table: "Settings", type: "REAL", nullable: true);
            migrationBuilder.AddColumn<double?>(name: "ScriptDesignerTop",    table: "Settings", type: "REAL", nullable: true);
            migrationBuilder.AddColumn<double>( name: "ScriptDesignerWidth",  table: "Settings", type: "REAL", nullable: false, defaultValue: 960.0);
            migrationBuilder.AddColumn<double>( name: "ScriptDesignerHeight", table: "Settings", type: "REAL", nullable: false, defaultValue: 700.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ScriptDesignerLeft",   table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptDesignerTop",    table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptDesignerWidth",  table: "Settings");
            migrationBuilder.DropColumn(name: "ScriptDesignerHeight", table: "Settings");
        }
    }
}
