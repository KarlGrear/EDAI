using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class SplitTitleDisplayMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new boolean columns first so the data migration SQL can reference them
            migrationBuilder.AddColumn<bool>(
                name: "DisplayTitle",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AnnounceTitle",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            // Migrate from old enum: None=0, Display=1, Announce=2, Both=3
            migrationBuilder.Sql("UPDATE EventConfigurations SET DisplayTitle  = CASE WHEN TitleDisplayMode IN (1, 3) THEN 1 ELSE 0 END");
            migrationBuilder.Sql("UPDATE EventConfigurations SET AnnounceTitle = CASE WHEN TitleDisplayMode IN (2, 3) THEN 1 ELSE 0 END");

            migrationBuilder.DropColumn(
                name: "TitleDisplayMode",
                table: "EventConfigurations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TitleDisplayMode",
                table: "EventConfigurations",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Reconstruct enum: None=0, Display=1, Announce=2, Both=3
            migrationBuilder.Sql(@"UPDATE EventConfigurations SET TitleDisplayMode =
                CASE
                    WHEN DisplayTitle = 1 AND AnnounceTitle = 1 THEN 3
                    WHEN DisplayTitle = 1 THEN 1
                    WHEN AnnounceTitle = 1 THEN 2
                    ELSE 0
                END");

            migrationBuilder.DropColumn(name: "AnnounceTitle", table: "EventConfigurations");
            migrationBuilder.DropColumn(name: "DisplayTitle",  table: "EventConfigurations");
        }
    }
}
