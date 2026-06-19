using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

public partial class AddCvUploadBatchProcessing : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE cv_files
                DROP CONSTRAINT IF EXISTS cv_files_file_type_check;

            ALTER TABLE cv_files
                ADD CONSTRAINT cv_files_file_type_check
                CHECK (file_type IN ('pdf', 'docx', 'doc'));
            """);

        migrationBuilder.CreateTable(
            name: "cv_upload_batches",
            columns: table => new
            {
                id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                uploaded_by = table.Column<long>(type: "bigint", nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                total_files = table.Column<int>(type: "integer", nullable: false),
                completed_files = table.Column<int>(type: "integer", nullable: false),
                failed_files = table.Column<int>(type: "integer", nullable: false),
                cancelled_files = table.Column<int>(type: "integer", nullable: false),
                processing_files = table.Column<int>(type: "integer", nullable: false),
                pending_files = table.Column<int>(type: "integer", nullable: false),
                request_ids = table.Column<string>(type: "text", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cv_upload_batches", x => x.id);
                table.ForeignKey(
                    name: "FK_cv_upload_batches_users_uploaded_by",
                    column: x => x.uploaded_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_cv_upload_batches_status",
            table: "cv_upload_batches",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "IX_cv_upload_batches_uploaded_by",
            table: "cv_upload_batches",
            column: "uploaded_by");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "cv_upload_batches");

        migrationBuilder.Sql("""
            ALTER TABLE cv_files
                DROP CONSTRAINT IF EXISTS cv_files_file_type_check;

            ALTER TABLE cv_files
                ADD CONSTRAINT cv_files_file_type_check
                CHECK (file_type IN ('pdf', 'docx'));
            """);
    }
}
