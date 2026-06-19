using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCvWorkTypeAndNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "note",
                table: "cv_infos",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "work_type",
                table: "cv_infos",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "cv_infos_work_type_check",
                table: "cv_infos",
                sql: "work_type IS NULL OR work_type IN ('Remote', 'Full-time', 'Part-time')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "cv_infos_work_type_check",
                table: "cv_infos");

            migrationBuilder.DropColumn(
                name: "note",
                table: "cv_infos");

            migrationBuilder.DropColumn(
                name: "work_type",
                table: "cv_infos");
        }
    }
}
