using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PredictLottoNZ.Migrations
{
    public partial class AddAdvancedLLMEntities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "ConfidenceScore",
                table: "Predictions",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasoningExplanation",
                table: "Predictions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TargetDrawDate",
                table: "Predictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ModelVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ModelVersionNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationAccuracy = table.Column<double>(type: "double precision", nullable: true),
                    ModelPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModelVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PredictionAccuracy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredictionId = table.Column<int>(type: "integer", nullable: false),
                    ActualDrawId = table.Column<int>(type: "integer", nullable: false),
                    ExactMatches = table.Column<int>(type: "integer", nullable: false),
                    PartialMatches = table.Column<int>(type: "integer", nullable: false),
                    ProximityScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallAccuracy = table.Column<double>(type: "double precision", nullable: false),
                    AnalyzedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionAccuracy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionAccuracy_LottoDraws_ActualDrawId",
                        column: x => x.ActualDrawId,
                        principalTable: "LottoDraws",
                        principalColumn: "Draw",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionAccuracy_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrainingRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TrainingDataSize = table.Column<int>(type: "integer", nullable: false),
                    FinalAccuracy = table.Column<double>(type: "double precision", nullable: true),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    ResultingModelVersionId = table.Column<int>(type: "integer", nullable: true),
                    TrainingParameters = table.Column<string>(type: "text", nullable: true),
                    TrainingMetrics = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingRuns_ModelVersions_ResultingModelVersionId",
                        column: x => x.ResultingModelVersionId,
                        principalTable: "ModelVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                name: "IX_PredictionAccuracy_ActualDrawId",
                table: "PredictionAccuracy",
                column: "ActualDrawId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_AnalyzedAt",
                table: "PredictionAccuracy",
                column: "AnalyzedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_PredictionId",
                table: "PredictionAccuracy",
                column: "PredictionId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_ProviderName",
                table: "PredictionAccuracy",
                column: "ProviderName");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionAccuracy_ProviderName_AnalyzedAt",
                table: "PredictionAccuracy",
                columns: new[] { "ProviderName", "AnalyzedAt" });

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
                name: "IX_TrainingRuns_ResultingModelVersionId",
                table: "TrainingRuns",
                column: "ResultingModelVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_StartedAt",
                table: "TrainingRuns",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingRuns_Status",
                table: "TrainingRuns",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionAccuracy");

            migrationBuilder.DropTable(
                name: "TrainingRuns");

            migrationBuilder.DropTable(
                name: "ModelVersions");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "ReasoningExplanation",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "TargetDrawDate",
                table: "Predictions");
        }
    }
}
