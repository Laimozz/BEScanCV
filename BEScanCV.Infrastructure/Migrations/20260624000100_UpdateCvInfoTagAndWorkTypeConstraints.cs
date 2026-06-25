using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BEScanCvDbContext))]
    [Migration("20260624000100_UpdateCvInfoTagAndWorkTypeConstraints")]
    public partial class UpdateCvInfoTagAndWorkTypeConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE cv_infos DROP CONSTRAINT IF EXISTS cv_infos_tag_check;");
            migrationBuilder.Sql("ALTER TABLE cv_infos DROP CONSTRAINT IF EXISTS cv_infos_work_type_check;");

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET tag = CASE lower(tag)
                    WHEN 'new' THEN 'new'
                    WHEN 'contracted' THEN 'contacted'
                    WHEN 'contacted' THEN 'contacted'
                    WHEN 'in-process' THEN 'in-process'
                    WHEN 'rejected' THEN 'rejected'
                    WHEN 'hired' THEN 'hired'
                    ELSE 'new'
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET work_type = CASE lower(work_type)
                    WHEN 'remote' THEN 'remote'
                    WHEN 'in-house' THEN 'in-house'
                    WHEN 'onsite' THEN 'onsite'
                    ELSE NULL
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "tag",
                table: "cv_infos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "new",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "New");

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_tag_check",
                table: "cv_infos",
                sql: "tag IN ('new', 'contacted', 'in-process', 'rejected', 'hired')");

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_work_type_check",
                table: "cv_infos",
                sql: "work_type IS NULL OR work_type IN ('remote', 'in-house', 'onsite')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE cv_infos DROP CONSTRAINT IF EXISTS cv_infos_tag_check;");
            migrationBuilder.Sql("ALTER TABLE cv_infos DROP CONSTRAINT IF EXISTS cv_infos_work_type_check;");

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET tag = CASE tag
                    WHEN 'new' THEN 'New'
                    WHEN 'contacted' THEN 'Contracted'
                    WHEN 'in-process' THEN 'In-Process'
                    WHEN 'rejected' THEN 'Rejected'
                    WHEN 'hired' THEN 'Hired'
                    ELSE 'New'
                END;
                """);

            migrationBuilder.Sql(
                """
                UPDATE cv_infos
                SET work_type = CASE work_type
                    WHEN 'remote' THEN 'Remote'
                    ELSE NULL
                END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "tag",
                table: "cv_infos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "New",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "new");

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_tag_check",
                table: "cv_infos",
                sql: "tag IN ('New', 'Contracted', 'In-Process', 'Rejected', 'Hired')");

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_work_type_check",
                table: "cv_infos",
                sql: "work_type IS NULL OR work_type IN ('Remote', 'Full-time', 'Part-time')");
        }
    }
}
