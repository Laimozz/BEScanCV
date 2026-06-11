using BEScanCV.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BEScanCV.Infrastructure.Migrations;

[DbContext(typeof(BEScanCvDbContext))]
[Migration("20260611000100_SimplifyCvSkillsSchema")]
public partial class SimplifyCvSkillsSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS cv_skills;
            DROP TABLE IF EXISTS skills;

            CREATE TABLE cv_skills (
                id bigserial PRIMARY KEY,
                cv_infos_id bigint NOT NULL,
                name character varying(100) NOT NULL,
                years_of_experience numeric(4, 1),
                CONSTRAINT fk_cv_skills_cv_infos_cv_infos_id
                    FOREIGN KEY (cv_infos_id)
                    REFERENCES cv_infos(id)
                    ON DELETE CASCADE
            );

            CREATE INDEX ix_cv_skills_cv_infos_id ON cv_skills(cv_infos_id);
            CREATE INDEX ix_cv_skills_name ON cv_skills(name);
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DROP TABLE IF EXISTS cv_skills;

            CREATE TABLE skills (
                id bigserial PRIMARY KEY,
                name character varying(100) NOT NULL,
                created_at timestamp with time zone NOT NULL
            );

            ALTER TABLE skills
                ADD CONSTRAINT ak_skills_name UNIQUE (name);

            CREATE TABLE cv_skills (
                cv_info_id bigint NOT NULL,
                skill_id bigint NOT NULL,
                confidence_score numeric(5, 2),
                years_of_experience numeric(4, 1),
                CONSTRAINT pk_cv_skills PRIMARY KEY (cv_info_id, skill_id),
                CONSTRAINT fk_cv_skills_cv_infos_cv_info_id
                    FOREIGN KEY (cv_info_id)
                    REFERENCES cv_infos(id)
                    ON DELETE CASCADE,
                CONSTRAINT fk_cv_skills_skills_skill_id
                    FOREIGN KEY (skill_id)
                    REFERENCES skills(id)
                    ON DELETE CASCADE
            );

            CREATE INDEX ix_cv_skills_skill_id ON cv_skills(skill_id);
            """);
    }
}
