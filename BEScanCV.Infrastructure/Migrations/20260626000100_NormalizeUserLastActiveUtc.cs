using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(BEScanCvDbContext))]
    [Migration("20260626000100_NormalizeUserLastActiveUtc")]
    public partial class NormalizeUserLastActiveUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE users
                SET "LastActive" = COALESCE(updated_at AT TIME ZONE 'UTC', now())
                WHERE "LastActive" = '-infinity'::timestamp with time zone
                   OR "LastActive" = 'infinity'::timestamp with time zone;
                """);

            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ALTER COLUMN "LastActive" SET DEFAULT now();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                ALTER TABLE users
                ALTER COLUMN "LastActive" SET DEFAULT '-infinity'::timestamp with time zone;
                """);
        }
    }
}
