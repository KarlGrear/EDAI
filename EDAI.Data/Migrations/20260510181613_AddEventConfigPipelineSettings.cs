using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventConfigPipelineSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColor",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldDefaultValue: "#FF6D00");

            migrationBuilder.AlterColumn<double>(
                name: "FontSize",
                table: "Settings",
                type: "REAL",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldDefaultValue: 14.0);

            migrationBuilder.AddColumn<string>(
                name: "ModelOverride",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendFullTriggerEvent",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "SendToAi",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelOverride",
                table: "EventConfigurations");

            migrationBuilder.DropColumn(
                name: "SendFullTriggerEvent",
                table: "EventConfigurations");

            migrationBuilder.DropColumn(
                name: "SendToAi",
                table: "EventConfigurations");

            migrationBuilder.AlterColumn<string>(
                name: "PrimaryColor",
                table: "Settings",
                type: "TEXT",
                nullable: false,
                defaultValue: "#FF6D00",
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<double>(
                name: "FontSize",
                table: "Settings",
                type: "REAL",
                nullable: false,
                defaultValue: 14.0,
                oldClrType: typeof(double),
                oldType: "REAL");
        }
    }
}
