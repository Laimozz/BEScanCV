using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCvWorkExperienceCertificationsAndBatchItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_marked",
                table: "cv_infos",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "tag",
                table: "cv_infos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "New");

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET is_marked = CASE
                    WHEN status = 'FAVORITE' THEN TRUE
                    ELSE FALSE
                END;
                """);

            migrationBuilder.DropColumn(
                name: "status",
                table: "cv_infos");

            migrationBuilder.CreateTable(
                name: "batch_upload_items",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    cv_upload_batch_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "QUEUE"),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_batch_upload_items", x => x.id);
                    table.CheckConstraint("batch_upload_items_status_check", "status IN ('QUEUE', 'PROCESSING', 'COMPLETED', 'FAILED')");
                    table.ForeignKey(
                        name: "FK_batch_upload_items_cv_upload_batches_cv_upload_batch_id",
                        column: x => x.cv_upload_batch_id,
                        principalTable: "cv_upload_batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cv_certification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    cv_info_id = table.Column<long>(type: "bigint", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cv_certification", x => x.id);
                    table.ForeignKey(
                        name: "FK_cv_certification_cv_infos_cv_info_id",
                        column: x => x.cv_info_id,
                        principalTable: "cv_infos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_experience",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    cv_info_id = table.Column<long>(type: "bigint", nullable: false),
                    company = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    position = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    duration = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    responsibility = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_experience", x => x.id);
                    table.ForeignKey(
                        name: "FK_work_experience_cv_infos_cv_info_id",
                        column: x => x.cv_info_id,
                        principalTable: "cv_infos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_tag_check",
                table: "cv_infos",
                sql: "tag IN ('New', 'Contracted', 'In-Process', 'Rejected', 'Hired')");

            migrationBuilder.CreateIndex(
                name: "IX_batch_upload_items_cv_upload_batch_id",
                table: "batch_upload_items",
                column: "cv_upload_batch_id");

            migrationBuilder.CreateIndex(
                name: "IX_batch_upload_items_status",
                table: "batch_upload_items",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_cv_certification_cv_info_id",
                table: "cv_certification",
                column: "cv_info_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_experience_cv_info_id",
                table: "work_experience",
                column: "cv_info_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "batch_upload_items");

            migrationBuilder.DropTable(
                name: "cv_certification");

            migrationBuilder.DropTable(
                name: "work_experience");

            migrationBuilder.DropCheckConstraint(
                name: "cv_infos_tag_check",
                table: "cv_infos");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "cv_infos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NOT_FAVORITE");

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET status = CASE
                    WHEN is_marked THEN 'FAVORITE'
                    ELSE 'NOT_FAVORITE'
                END;
                """);

            migrationBuilder.DropColumn(
                name: "is_marked",
                table: "cv_infos");

            migrationBuilder.DropColumn(
                name: "tag",
                table: "cv_infos");
        }
    }
}
