using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCvQualityScoreFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<JsonDocument>(
                name: "quality_details",
                table: "cv_infos",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "quality_reason",
                table: "cv_infos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "quality_score",
                table: "cv_infos",
                type: "double precision",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cv_files_ai_document_id",
                table: "cv_files",
                column: "ai_document_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cv_files_ai_document_id",
                table: "cv_files");

            migrationBuilder.DropColumn(
                name: "quality_details",
                table: "cv_infos");

            migrationBuilder.DropColumn(
                name: "quality_reason",
                table: "cv_infos");

            migrationBuilder.DropColumn(
                name: "quality_score",
                table: "cv_infos");
        }
    }
}
