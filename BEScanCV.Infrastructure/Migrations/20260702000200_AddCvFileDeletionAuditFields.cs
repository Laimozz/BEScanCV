using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260702000200_AddCvFileDeletionAuditFields")]
public partial class AddCvFileDeletionAuditFields : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "deleted_at",
            table: "cv_files",
            type: "timestamp with time zone",
            nullable: true);

        migrationBuilder.AddColumn<long>(
            name: "deleted_by",
            table: "cv_files",
            type: "bigint",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "deleted_at",
            table: "cv_files");

        migrationBuilder.DropColumn(
            name: "deleted_by",
            table: "cv_files");
    }
}
