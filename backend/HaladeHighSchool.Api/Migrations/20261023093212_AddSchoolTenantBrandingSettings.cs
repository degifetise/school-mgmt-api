using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolTenantBrandingSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Schools",
                type: "character varying(9)",
                maxLength: 9,
                nullable: false,
                defaultValueSql: "to_char(CURRENT_DATE, 'YYYY') || '-' || to_char(CURRENT_DATE + INTERVAL '1 year', 'YYYY')");

            migrationBuilder.AddColumn<string>(
                name: "HeroDescription",
                table: "Schools",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "Lessons, weighted assessments and report cards in one place.");

            migrationBuilder.AddColumn<string>(
                name: "HeroSubtitle",
                table: "Schools",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "Nursery through Grade 12, one portal.");

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Schools",
                type: "character varying(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#1e3a8a");

            migrationBuilder.Sql(
                """
                UPDATE "Schools" AS school
                SET "AcademicYear" = setting."Value"
                FROM "SystemSettings" AS setting
                WHERE setting."Key" = 'AcademicYear'
                  AND setting."Value" ~ '^[0-9]{4}-[0-9]{4}$'
                  AND substring(setting."Value" FROM 6 FOR 4)::integer =
                      substring(setting."Value" FROM 1 FOR 4)::integer + 1;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "HeroDescription",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "HeroSubtitle",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Schools");
        }
    }
}
