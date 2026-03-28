using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PredictLottoNZ.Migrations
{
    /// <inheritdoc />
    public partial class AddPowerballAndMatchingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Predictions_Numbers",
                table: "Predictions");

            migrationBuilder.AddColumn<bool>(
                name: "IsMatched",
                table: "Predictions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "MatchedAt",
                table: "Predictions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MatchedDrawId",
                table: "Predictions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Powerball",
                table: "Predictions",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_IsMatched",
                table: "Predictions",
                column: "IsMatched");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_MatchedDrawId",
                table: "Predictions",
                column: "MatchedDrawId");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_Numbers_Powerball",
                table: "Predictions",
                columns: new[] { "Number1", "Number2", "Number3", "Number4", "Number5", "Number6", "Powerball" });

            migrationBuilder.AddForeignKey(
                name: "FK_Predictions_LottoDraws_MatchedDrawId",
                table: "Predictions",
                column: "MatchedDrawId",
                principalTable: "LottoDraws",
                principalColumn: "Draw",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Predictions_LottoDraws_MatchedDrawId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_IsMatched",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_MatchedDrawId",
                table: "Predictions");

            migrationBuilder.DropIndex(
                name: "IX_Predictions_Numbers_Powerball",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "IsMatched",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "MatchedAt",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "MatchedDrawId",
                table: "Predictions");

            migrationBuilder.DropColumn(
                name: "Powerball",
                table: "Predictions");

            migrationBuilder.CreateIndex(
                name: "IX_Predictions_Numbers",
                table: "Predictions",
                columns: new[] { "Number1", "Number2", "Number3", "Number4", "Number5", "Number6" });
        }
    }
}
