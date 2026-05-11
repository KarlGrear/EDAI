using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TriggerCondition",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayCondition",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnnounceCondition",
                table: "EventConfigurations",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TriggerCondition",
                table: "EventConfigurations");

            migrationBuilder.DropColumn(
                name: "DisplayCondition",
                table: "EventConfigurations");

            migrationBuilder.DropColumn(
                name: "AnnounceCondition",
                table: "EventConfigurations");
        }
    }
}
