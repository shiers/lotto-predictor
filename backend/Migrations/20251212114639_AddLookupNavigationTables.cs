using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PredictLottoNZ.Migrations
{
    /// <inheritdoc />
    public partial class AddLookupNavigationTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentPhase",
                table: "TrainingRuns",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TriggerReason",
                table: "TrainingRuns",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Bookmarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    DrawNumber = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookmarks_LottoDraws_DrawNumber",
                        column: x => x.DrawNumber,
                        principalTable: "LottoDraws",
                        principalColumn: "Draw",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExportId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ExportType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Parameters = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberFrequencies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    TotalOccurrences = table.Column<int>(type: "integer", nullable: false),
                    LastAppearance = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FirstAppearance = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LongestGap = table.Column<int>(type: "integer", nullable: false),
                    AverageFrequency = table.Column<double>(type: "double precision", nullable: false),
                    LastCalculated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberFrequencies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "NumberOccurrences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DrawNumber = table.Column<int>(type: "integer", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    DrawDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsBonus = table.Column<bool>(type: "boolean", nullable: false),
                    IsPowerball = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberOccurrences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NumberOccurrences_LottoDraws_DrawNumber",
                        column: x => x.DrawNumber,
                        principalTable: "LottoDraws",
                        principalColumn: "Draw",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SearchType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SearchCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExecutionTime = table.Column<TimeSpan>(type: "interval", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_BonusNumber",
                table: "LottoDraws",
                column: "BonusNumber");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_Date_Draw",
                table: "LottoDraws",
                columns: new[] { "Date", "Draw" });

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_Powerball",
                table: "LottoDraws",
                column: "Powerball");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber1",
                table: "LottoDraws",
                column: "WinningNumber1");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber2",
                table: "LottoDraws",
                column: "WinningNumber2");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber3",
                table: "LottoDraws",
                column: "WinningNumber3");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber4",
                table: "LottoDraws",
                column: "WinningNumber4");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber5",
                table: "LottoDraws",
                column: "WinningNumber5");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumber6",
                table: "LottoDraws",
                column: "WinningNumber6");

            migrationBuilder.CreateIndex(
                name: "IX_LottoDraws_WinningNumbers_Composite",
                table: "LottoDraws",
                columns: new[] { "WinningNumber1", "WinningNumber2", "WinningNumber3", "WinningNumber4", "WinningNumber5", "WinningNumber6" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_CreatedAt",
                table: "Bookmarks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_DrawNumber",
                table: "Bookmarks",
                column: "DrawNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_CreatedAt",
                table: "Bookmarks",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_UserId_DrawNumber_Unique",
                table: "Bookmarks",
                columns: new[] { "UserId", "DrawNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_CompletedAt",
                table: "ExportJobs",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_CreatedAt",
                table: "ExportJobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_ExportId_Unique",
                table: "ExportJobs",
                column: "ExportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_Status",
                table: "ExportJobs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_UserId",
                table: "ExportJobs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_UserId_CreatedAt",
                table: "ExportJobs",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_UserId_Status",
                table: "ExportJobs",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberFrequencies_LastAppearance",
                table: "NumberFrequencies",
                column: "LastAppearance");

            migrationBuilder.CreateIndex(
                name: "IX_NumberFrequencies_LastCalculated",
                table: "NumberFrequencies",
                column: "LastCalculated");

            migrationBuilder.CreateIndex(
                name: "IX_NumberFrequencies_Number_Unique",
                table: "NumberFrequencies",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumberFrequencies_TotalOccurrences",
                table: "NumberFrequencies",
                column: "TotalOccurrences");

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_DrawDate",
                table: "NumberOccurrences",
                column: "DrawDate");

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_DrawNumber",
                table: "NumberOccurrences",
                column: "DrawNumber");

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_DrawNumber_Position",
                table: "NumberOccurrences",
                columns: new[] { "DrawNumber", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Number",
                table: "NumberOccurrences",
                column: "Number");

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Number_DrawDate",
                table: "NumberOccurrences",
                columns: new[] { "Number", "DrawDate" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Number_Type",
                table: "NumberOccurrences",
                columns: new[] { "Number", "IsBonus", "IsPowerball" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Position",
                table: "NumberOccurrences",
                column: "Position");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_SearchedAt",
                table: "SearchHistory",
                column: "SearchedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_SearchType",
                table: "SearchHistory",
                column: "SearchType");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_UserId",
                table: "SearchHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_UserId_SearchedAt",
                table: "SearchHistory",
                columns: new[] { "UserId", "SearchedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchHistory_UserId_SearchType",
                table: "SearchHistory",
                columns: new[] { "UserId", "SearchType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookmarks");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "NumberFrequencies");

            migrationBuilder.DropTable(
                name: "NumberOccurrences");

            migrationBuilder.DropTable(
                name: "SearchHistory");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_BonusNumber",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_Date_Draw",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_Powerball",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber1",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber2",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber3",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber4",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber5",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumber6",
                table: "LottoDraws");

            migrationBuilder.DropIndex(
                name: "IX_LottoDraws_WinningNumbers_Composite",
                table: "LottoDraws");

            migrationBuilder.DropColumn(
                name: "CurrentPhase",
                table: "TrainingRuns");

            migrationBuilder.DropColumn(
                name: "TriggerReason",
                table: "TrainingRuns");
        }
    }
}
