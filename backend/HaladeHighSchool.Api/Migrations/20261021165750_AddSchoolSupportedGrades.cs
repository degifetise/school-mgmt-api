using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolSupportedGrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "AspNetUsers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateTable(
                name: "Schools",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SupportedGrades = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schools", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Schools",
                columns: new[] { "Id", "Name", "SupportedGrades" },
                values: new object[]
                {
                    new Guid("11111111-1111-1111-1111-111111111111"),
                    "School Management System",
                    new[]
                    {
                        "Nursery", "KG 1", "KG 2", "KG 3",
                        "Grade 1", "Grade 2", "Grade 3", "Grade 4", "Grade 5", "Grade 6",
                        "Grade 7", "Grade 8", "Grade 9", "Grade 10", "Grade 11", "Grade 12"
                    }
                });

            migrationBuilder.Sql(
                "UPDATE \"Schools\" SET \"SupportedGrades\" = (" +
                "SELECT COALESCE(array_agg(\"Name\" ORDER BY \"Level\"), ARRAY[]::text[]) " +
                "FROM \"GradeLevels\") " +
                "WHERE \"Id\" = '11111111-1111-1111-1111-111111111111';");

            migrationBuilder.CreateTable(
                name: "SchoolGradeSections",
                columns: table => new
                {
                    SchoolId = table.Column<Guid>(type: "uuid", nullable: false),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolGradeSections", x => new { x.SchoolId, x.GradeLevelId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_SchoolGradeSections_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchoolGradeSections_Schools_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "Schools",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SchoolGradeSections_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                "INSERT INTO \"SchoolGradeSections\" (\"SchoolId\", \"GradeLevelId\", \"SectionId\") " +
                "SELECT '11111111-1111-1111-1111-111111111111', \"GradeLevelId\", \"SectionId\" " +
                "FROM \"GradeLevelSections\";");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_SchoolId",
                table: "AspNetUsers",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGradeSections_GradeLevelId",
                table: "SchoolGradeSections",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGradeSections_School_Grade",
                table: "SchoolGradeSections",
                columns: new[] { "SchoolId", "GradeLevelId" });

            migrationBuilder.CreateIndex(
                name: "IX_SchoolGradeSections_SectionId",
                table: "SchoolGradeSections",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetUsers_Schools_SchoolId",
                table: "AspNetUsers",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetUsers_Schools_SchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "SchoolGradeSections");

            migrationBuilder.DropTable(
                name: "Schools");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_SchoolId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "AspNetUsers");
        }
    }
}
