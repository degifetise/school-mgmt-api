using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGradeLevelSectionMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_GradeLevelId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentRegistrationRequests_GradeLevelId",
                table: "StudentRegistrationRequests");

            migrationBuilder.CreateTable(
                name: "GradeLevelSections",
                columns: table => new
                {
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GradeLevelSections", x => new { x.GradeLevelId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_GradeLevelSections_GradeLevels_GradeLevelId",
                        column: x => x.GradeLevelId,
                        principalTable: "GradeLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GradeLevelSections_Sections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "Sections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_GradeLevelId_SectionId",
                table: "Students",
                columns: new[] { "GradeLevelId", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrationRequests_GradeLevelId_SectionId",
                table: "StudentRegistrationRequests",
                columns: new[] { "GradeLevelId", "SectionId" });

            migrationBuilder.Sql("""
                INSERT INTO "GradeLevelSections" ("GradeLevelId", "SectionId")
                SELECT grades."Id", sections."Id"
                FROM "GradeLevels" AS grades
                CROSS JOIN "Sections" AS sections;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_GradeLevelSections_SectionId",
                table: "GradeLevelSections",
                column: "SectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentRegistrationRequests_GradeLevelSections_GradeLevelId~",
                table: "StudentRegistrationRequests",
                columns: new[] { "GradeLevelId", "SectionId" },
                principalTable: "GradeLevelSections",
                principalColumns: new[] { "GradeLevelId", "SectionId" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_GradeLevelSections_GradeLevelId_SectionId",
                table: "Students",
                columns: new[] { "GradeLevelId", "SectionId" },
                principalTable: "GradeLevelSections",
                principalColumns: new[] { "GradeLevelId", "SectionId" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentRegistrationRequests_GradeLevelSections_GradeLevelId~",
                table: "StudentRegistrationRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_GradeLevelSections_GradeLevelId_SectionId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "GradeLevelSections");

            migrationBuilder.DropIndex(
                name: "IX_Students_GradeLevelId_SectionId",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_StudentRegistrationRequests_GradeLevelId_SectionId",
                table: "StudentRegistrationRequests");

            migrationBuilder.CreateIndex(
                name: "IX_Students_GradeLevelId",
                table: "Students",
                column: "GradeLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrationRequests_GradeLevelId",
                table: "StudentRegistrationRequests",
                column: "GradeLevelId");
        }
    }
}
