using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PredictLottoNZ.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionScoreTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastScoreUpdate",
                table: "Predictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UpdatedScore",
                table: "Predictions",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PredictionScoreHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PredictionId = table.Column<int>(type: "integer", nullable: false),
                    OriginalScore = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedScore = table.Column<double>(type: "double precision", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdateReason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TriggeringDrawId = table.Column<int>(type: "integer", nullable: false),
                    ExactMatches = table.Column<int>(type: "integer", nullable: false),
                    PartialMatches = table.Column<int>(type: "integer", nullable: false),
                    ProximityScore = table.Column<double>(type: "double precision", nullable: false),
                    OverallAccuracy = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictionScoreHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictionScoreHistory_LottoDraws_TriggeringDrawId",
                        column: x => x.TriggeringDrawId,
                        principalTable: "LottoDraws",
                        principalColumn: "Draw",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PredictionScoreHistory_Predictions_PredictionId",
                        column: x => x.PredictionId,
                        principalTable: "Predictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictionScoreHistory_PredictionId",
                table: "PredictionScoreHistory",
                column: "PredictionId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionScoreHistory_TriggeringDrawId",
                table: "PredictionScoreHistory",
                column: "TriggeringDrawId");

            migrationBuilder.CreateIndex(
                name: "IX_PredictionScoreHistory_UpdatedAt",
                table: "PredictionScoreHistory",
                column: "UpdatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictionScoreHistory");

            migrationBuilder.DropColumn(
                name: "LastScoreUpdate",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "UpdatedScore",
                table: "Predictions");
        }
    }
}
