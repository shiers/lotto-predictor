using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PredictLottoNZ.Migrations
{
    /// <inheritdoc />
    public partial class FixNumberOccurrenceRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmarks_LottoDraws_DrawNumber",
                table: "Bookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK_PredictionAccuracy_LottoDraws_ActualDrawId",
                table: "PredictionAccuracy");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRuns_ModelVersions_ResultingModelVersionId",
                table: "TrainingRuns");

            migrationBuilder.DropTable(
                name: "SearchHistory");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_CompletedAt",
                table: "TrainingRuns");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_ProviderName",
                table: "TrainingRuns");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_ProviderName_Status",
                table: "TrainingRuns");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_StartedAt",
                table: "TrainingRuns");

            migrationBuilder.DropIndex(
                name: "IX_TrainingRuns_Status",
                table: "TrainingRuns");

            migrationBuilder.DropIndex(
                name: "IX_PredictionAccuracy_AnalyzedAt",
                table: "PredictionAccuracy");

            migrationBuilder.DropIndex(
                name: "IX_PredictionAccuracy_ProviderName",
                table: "PredictionAccuracy");

            migrationBuilder.DropIndex(
                name: "IX_PredictionAccuracy_ProviderName_AnalyzedAt",
                table: "PredictionAccuracy");

            migrationBuilder.DropIndex(
                name: "IX_NumberOccurrences_DrawNumber_Position",
                table: "NumberOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_NumberOccurrences_Number_Type",
                table: "NumberOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_NumberOccurrences_Position",
                table: "NumberOccurrences");

            migrationBuilder.DropIndex(
                name: "IX_NumberFrequencies_LastAppearance",
                table: "NumberFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_NumberFrequencies_LastCalculated",
                table: "NumberFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_NumberFrequencies_Number_Unique",
                table: "NumberFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_NumberFrequencies_TotalOccurrences",
                table: "NumberFrequencies");

            migrationBuilder.DropIndex(
                name: "IX_ModelVersions_CreatedAt",
                table: "ModelVersions");

            migrationBuilder.DropIndex(
                name: "IX_ModelVersions_DeployedAt",
                table: "ModelVersions");

            migrationBuilder.DropIndex(
                name: "IX_ModelVersions_IsActive",
                table: "ModelVersions");

            migrationBuilder.DropIndex(
                name: "IX_ModelVersions_ProviderName",
                table: "ModelVersions");

            migrationBuilder.DropIndex(
                name: "IX_ModelVersions_ProviderName_IsActive",
                table: "ModelVersions");

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

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_CompletedAt",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_CreatedAt",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_ExportId_Unique",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_Status",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_UserId",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_UserId_CreatedAt",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_ExportJobs_UserId_Status",
                table: "ExportJobs");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_CreatedAt",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_DrawNumber",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_CreatedAt",
                table: "Bookmarks");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_UserId_DrawNumber_Unique",
                table: "Bookmarks");

            migrationBuilder.AlterColumn<string>(
                name: "ReasoningExplanation",
                table: "Predictions",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentGap",
                table: "NumberFrequencies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsCold",
                table: "NumberFrequencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHot",
                table: "NumberFrequencies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Percentage",
                table: "NumberFrequencies",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Draw1",
                table: "Bookmarks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "SearchConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SearchRequestJson = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastUsed = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    IsPublic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchConfigurations", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookmarks_Draw1",
                table: "Bookmarks",
                column: "Draw1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_LottoDraws_Draw1",
                table: "Bookmarks",
                column: "Draw1",
                principalTable: "LottoDraws",
                principalColumn: "Draw",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PredictionAccuracy_LottoDraws_ActualDrawId",
                table: "PredictionAccuracy",
                column: "ActualDrawId",
                principalTable: "LottoDraws",
                principalColumn: "Draw",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRuns_ModelVersions_ResultingModelVersionId",
                table: "TrainingRuns",
                column: "ResultingModelVersionId",
                principalTable: "ModelVersions",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookmarks_LottoDraws_Draw1",
                table: "Bookmarks");

            migrationBuilder.DropForeignKey(
                name: "FK_PredictionAccuracy_LottoDraws_ActualDrawId",
                table: "PredictionAccuracy");

            migrationBuilder.DropForeignKey(
                name: "FK_TrainingRuns_ModelVersions_ResultingModelVersionId",
                table: "TrainingRuns");

            migrationBuilder.DropTable(
                name: "SearchConfigurations");

            migrationBuilder.DropIndex(
                name: "IX_Bookmarks_Draw1",
                table: "Bookmarks");

            migrationBuilder.DropColumn(
                name: "CurrentGap",
                table: "NumberFrequencies");

            migrationBuilder.DropColumn(
                name: "IsCold",
                table: "NumberFrequencies");

            migrationBuilder.DropColumn(
                name: "IsHot",
                table: "NumberFrequencies");

            migrationBuilder.DropColumn(
                name: "Percentage",
                table: "NumberFrequencies");

            migrationBuilder.DropColumn(
                name: "Draw1",
                table: "Bookmarks");

            migrationBuilder.AlterColumn<string>(
                name: "ReasoningExplanation",
                table: "Predictions",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.CreateTable(
                name: "SearchHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ExecutionTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    SearchCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    SearchType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchHistory", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_CompletedAt",
                table: "TrainingRuns",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_ProviderName",
                table: "TrainingRuns",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_ProviderName_Status",
                table: "TrainingRuns",
                columns: new[] { "ProviderName", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_StartedAt",
                table: "TrainingRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_Status",
                table: "TrainingRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_AnalyzedAt",
                table: "PredictionAccuracy",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_ProviderName",
                table: "PredictionAccuracy",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_ProviderName_AnalyzedAt",
                table: "PredictionAccuracy",
                columns: new[] { "ProviderName", "AnalyzedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_DrawNumber_Position",
                table: "NumberOccurrences",
                columns: new[] { "DrawNumber", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Number_Type",
                table: "NumberOccurrences",
                columns: new[] { "Number", "IsBonus", "IsPowerball" });

            migrationBuilder.CreateIndex(
                name: "IX_NumberOccurrences_Position",
                table: "NumberOccurrences",
                column: "Position");

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
                name: "IX_ModelVersions_CreatedAt",
                table: "ModelVersions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVersions_DeployedAt",
                table: "ModelVersions",
                column: "DeployedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVersions_IsActive",
                table: "ModelVersions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVersions_ProviderName",
                table: "ModelVersions",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_ModelVersions_ProviderName_IsActive",
                table: "ModelVersions",
                columns: new[] { "ProviderName", "IsActive" });

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

            migrationBuilder.AddForeignKey(
                name: "FK_Bookmarks_LottoDraws_DrawNumber",
                table: "Bookmarks",
                column: "DrawNumber",
                principalTable: "LottoDraws",
                principalColumn: "Draw",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PredictionAccuracy_LottoDraws_ActualDrawId",
                table: "PredictionAccuracy",
                column: "ActualDrawId",
                principalTable: "LottoDraws",
                principalColumn: "Draw",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TrainingRuns_ModelVersions_ResultingModelVersionId",
                table: "TrainingRuns",
                column: "ResultingModelVersionId",
                principalTable: "ModelVersions",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
