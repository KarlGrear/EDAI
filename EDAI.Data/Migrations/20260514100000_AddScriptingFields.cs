using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProcessingType",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: false,
                defaultValue: "None");

            migrationBuilder.AddColumn<string>(
                name: "ProcessScript",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerConditionScript",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayConditionScript",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnounceConditionScript",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            // Migrate existing rows: configurations that had SendToAi=true get ProcessingType='Ai'.
            migrationBuilder.Sql("UPDATE EventConfigurations SET ProcessingType = 'Ai' WHERE SendToAi = 1;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "ProcessingType",         table: "EventConfigurations");
            migrationBuilder.DropColumn(name: "ProcessScript",          table: "EventConfigurations");
            migrationBuilder.DropColumn(name: "TriggerConditionScript", table: "EventConfigurations");
            migrationBuilder.DropColumn(name: "DisplayConditionScript", table: "EventConfigurations");
            migrationBuilder.DropColumn(name: "AnnounceConditionScript", table: "EventConfigurations");
        }
    }
}
