using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EDAI.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SessionHistories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CommanderName = table.Column<string>(type: "TEXT", nullable: false),
                    SessionStart = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SessionEnd = table.Column<DateTime>(type: "TEXT", nullable: true),
                    JournalFileName = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SessionHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OpenAiApiKeyEncrypted = table.Column<string>(type: "TEXT", nullable: true),
                    OpenAiModel = table.Column<string>(type: "TEXT", nullable: false),
                    TtsVoiceName = table.Column<string>(type: "TEXT", nullable: true),
                    TtsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    AlwaysOnTop = table.Column<bool>(type: "INTEGER", nullable: false),
                    TrayNotificationsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Theme = table.Column<string>(type: "TEXT", nullable: false),
                    WindowWidth = table.Column<double>(type: "REAL", nullable: false),
                    WindowHeight = table.Column<double>(type: "REAL", nullable: false),
                    WindowLeft = table.Column<double>(type: "REAL", nullable: true),
                    WindowTop = table.Column<double>(type: "REAL", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    TriggeringEvents = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryEvents = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryWaitTimeMs = table.Column<int>(type: "INTEGER", nullable: false),
                    Prompt = table.Column<string>(type: "TEXT", nullable: false),
                    ExpectedResultsSchema = table.Column<string>(type: "TEXT", nullable: true),
                    TitleDisplayMode = table.Column<int>(type: "INTEGER", nullable: false),
                    DisplayFields = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayKeys = table.Column<bool>(type: "INTEGER", nullable: false),
                    AnnounceFields = table.Column<string>(type: "TEXT", nullable: false),
                    AnnounceKeys = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowTrayNotification = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventConfigurations_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ResponseLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionHistoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    EventConfigurationId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TriggeringEventJson = table.Column<string>(type: "TEXT", nullable: false),
                    SecondaryEventsJson = table.Column<string>(type: "TEXT", nullable: true),
                    PromptSent = table.Column<string>(type: "TEXT", nullable: false),
                    RawAiResponse = table.Column<string>(type: "TEXT", nullable: false),
                    DisplayedOutput = table.Column<string>(type: "TEXT", nullable: true),
                    AnnouncedOutput = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResponseLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResponseLogs_EventConfigurations_EventConfigurationId",
                        column: x => x.EventConfigurationId,
                        principalTable: "EventConfigurations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResponseLogs_SessionHistories_SessionHistoryId",
                        column: x => x.SessionHistoryId,
                        principalTable: "SessionHistories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventConfigurations_CategoryId",
                table: "EventConfigurations",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseLogs_EventConfigurationId",
                table: "ResponseLogs",
                column: "EventConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_ResponseLogs_SessionHistoryId",
                table: "ResponseLogs",
                column: "SessionHistoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResponseLogs");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "EventConfigurations");

            migrationBuilder.DropTable(
                name: "SessionHistories");

            migrationBuilder.DropTable(
                name: "Categories");
        }
    }
}
