using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260616000100_UpdateCvFilesFileTypeConstraint")]
public partial class UpdateCvFilesFileTypeConstraint : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE cv_files
               SET file_type = lower(trim(file_type))
             WHERE file_type IS NOT NULL;

            UPDATE cv_files
               SET file_type = 'pdf'
             WHERE lower(file_type) NOT IN ('pdf', 'docx');

            ALTER TABLE cv_files
                DROP CONSTRAINT IF EXISTS cv_files_file_type_check;

            ALTER TABLE cv_files
                ADD CONSTRAINT cv_files_file_type_check
                CHECK (file_type IN ('pdf', 'docx'));
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE cv_files
                DROP CONSTRAINT IF EXISTS cv_files_file_type_check;
            """);
    }
}
