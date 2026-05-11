using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class NullableSessionHistoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Make SessionHistoryId optional so that response logs can be written
            // even when no active game session exists in SessionHistories.
            migrationBuilder.AlterColumn<int>(
                name: "SessionHistoryId",
                table: "ResponseLogs",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SessionHistoryId",
                table: "ResponseLogs",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
