using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260702000100_AddIsDeletedToCvFiles")]
public partial class AddIsDeletedToCvFiles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_deleted",
            table: "cv_files",
            type: "boolean",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("""
            UPDATE cv_files SET is_deleted = false WHERE is_deleted IS NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "is_deleted",
            table: "cv_files");
    }
}
