using System;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260610000100_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
                table.UniqueConstraint("AK_users_email", x => x.email);
            });

        migrationBuilder.CreateTable(
            name: "cv_files",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                uploaded_by = table.Column<long>(type: "bigint", nullable: false),
                original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                file_url = table.Column<string>(type: "text", nullable: false),
                file_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                file_size = table.Column<long>(type: "bigint", nullable: false),
                ai_document_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cv_files", x => x.id);
                table.ForeignKey(
                    name: "FK_cv_files_users_uploaded_by",
                    column: x => x.uploaded_by,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "refresh_tokens",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                user_id = table.Column<long>(type: "bigint", nullable: false),
                token_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_refresh_tokens", x => x.id);
                table.ForeignKey(
                    name: "FK_refresh_tokens_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "cv_infos",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                cv_file_id = table.Column<long>(type: "bigint", nullable: false),
                full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                date_of_birth = table.Column<DateOnly>(type: "date", nullable: true),
                address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                summary = table.Column<string>(type: "text", nullable: true),
                educations = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[] { }),
                certifications = table.Column<string[]>(type: "text[]", nullable: false, defaultValue: new string[] { }),
                status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NOT_FAVORITE"),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_cv_infos", x => x.id);
                table.ForeignKey(
                    name: "FK_cv_infos_cv_files_cv_file_id",
                    column: x => x.cv_file_id,
                    principalTable: "cv_files",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_cv_files_uploaded_by",
            table: "cv_files",
            column: "uploaded_by");

        migrationBuilder.CreateIndex(
            name: "IX_cv_infos_cv_file_id",
            table: "cv_infos",
            column: "cv_file_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_refresh_tokens_user_id",
            table: "refresh_tokens",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "cv_infos");
        migrationBuilder.DropTable(name: "refresh_tokens");
        migrationBuilder.DropTable(name: "cv_files");
        migrationBuilder.DropTable(name: "users");
    }
}
