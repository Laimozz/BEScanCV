using System.Text.Json;
using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260615000100_UpdateCvInfoJsonProfileSchema")]
public partial class UpdateCvInfoJsonProfileSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "total_experience_years",
            table: "cv_infos",
            type: "integer",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "raw_text",
            table: "cv_infos",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<JsonDocument>(
            name: "profile_data",
            table: "cv_infos",
            type: "jsonb",
            nullable: true);

        migrationBuilder.DropColumn(
            name: "certifications",
            table: "cv_infos");

        migrationBuilder.DropColumn(
            name: "years_of_experience",
            table: "cv_skills");

        migrationBuilder.Sql("""
            ALTER TABLE cv_infos
                ALTER COLUMN educations DROP DEFAULT;

            ALTER TABLE cv_infos
                ALTER COLUMN educations DROP NOT NULL;

            ALTER TABLE cv_infos
                ALTER COLUMN educations TYPE jsonb USING to_jsonb(educations);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            ALTER TABLE cv_infos
                ALTER COLUMN educations TYPE text[]
                USING CASE
                    WHEN educations IS NULL THEN ARRAY[]::text[]
                    ELSE ARRAY[educations::text]
                END;

            ALTER TABLE cv_infos
                ALTER COLUMN educations SET NOT NULL;
            """);

        migrationBuilder.AddColumn<string[]>(
            name: "certifications",
            table: "cv_infos",
            type: "text[]",
            nullable: false,
            defaultValue: new string[] { });

        migrationBuilder.AddColumn<decimal>(
            name: "years_of_experience",
            table: "cv_skills",
            type: "numeric(4,1)",
            precision: 4,
            scale: 1,
            nullable: true);

        migrationBuilder.DropColumn(
            name: "profile_data",
            table: "cv_infos");

        migrationBuilder.DropColumn(
            name: "raw_text",
            table: "cv_infos");

        migrationBuilder.DropColumn(
            name: "total_experience_years",
            table: "cv_infos");
    }
}
