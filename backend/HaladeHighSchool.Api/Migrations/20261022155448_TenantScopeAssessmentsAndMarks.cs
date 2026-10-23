using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class TenantScopeAssessmentsAndMarks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Marks_Assessments_AssessmentId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Marks_AssessmentId",
                table: "Marks");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Marks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Assessments",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Assessments" AS assessment
                SET "SchoolId" = app_user."SchoolId"
                FROM "Teachers" AS teacher
                JOIN "AspNetUsers" AS app_user ON app_user."Id" = teacher."UserId"
                WHERE assessment."TeacherId" = teacher."Id";

                UPDATE "Assessments" AS assessment
                SET "SchoolId" = ownership."SchoolId"
                FROM (
                    SELECT mark."AssessmentId",
                           min(app_user."SchoolId"::text)::uuid AS "SchoolId"
                    FROM "Marks" AS mark
                    JOIN "Students" AS student ON student."Id" = mark."StudentId"
                    JOIN "AspNetUsers" AS app_user ON app_user."Id" = student."UserId"
                    GROUP BY mark."AssessmentId"
                    HAVING count(DISTINCT app_user."SchoolId") = 1
                ) AS ownership
                WHERE assessment."Id" = ownership."AssessmentId"
                  AND assessment."SchoolId" IS NULL;

                UPDATE "Marks" AS mark
                SET "SchoolId" = app_user."SchoolId"
                FROM "Students" AS student
                JOIN "AspNetUsers" AS app_user ON app_user."Id" = student."UserId"
                WHERE mark."StudentId" = student."Id";

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Assessments" WHERE "SchoolId" IS NULL) THEN
                        RAISE EXCEPTION 'Tenant migration blocked: assign every assessment to a school before retrying.';
                    END IF;
                    IF EXISTS (
                        SELECT 1
                        FROM "Marks" AS mark
                        JOIN "Assessments" AS assessment ON assessment."Id" = mark."AssessmentId"
                        WHERE mark."SchoolId" IS NULL OR mark."SchoolId" <> assessment."SchoolId"
                    ) THEN
                        RAISE EXCEPTION 'Tenant migration blocked: resolve marks with missing or mismatched student/assessment school ownership before retrying.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolId",
                table: "Assessments",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolId",
                table: "Marks",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Assessments_SchoolId_Id",
                table: "Assessments",
                columns: new[] { "SchoolId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Marks_SchoolId",
                table: "Marks",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_SchoolId_AssessmentId",
                table: "Marks",
                columns: new[] { "SchoolId", "AssessmentId" });

            migrationBuilder.CreateIndex(
                name: "IX_Assessments_SchoolId",
                table: "Assessments",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Assessments_Schools_SchoolId",
                table: "Assessments",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Marks_Assessments_SchoolId_AssessmentId",
                table: "Marks",
                columns: new[] { "SchoolId", "AssessmentId" },
                principalTable: "Assessments",
                principalColumns: new[] { "SchoolId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Marks_Schools_SchoolId",
                table: "Marks",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Assessments_Schools_SchoolId",
                table: "Assessments");

            migrationBuilder.DropForeignKey(
                name: "FK_Marks_Assessments_SchoolId_AssessmentId",
                table: "Marks");

            migrationBuilder.DropForeignKey(
                name: "FK_Marks_Schools_SchoolId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Marks_SchoolId",
                table: "Marks");

            migrationBuilder.DropIndex(
                name: "IX_Marks_SchoolId_AssessmentId",
                table: "Marks");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Assessments_SchoolId_Id",
                table: "Assessments");

            migrationBuilder.DropIndex(
                name: "IX_Assessments_SchoolId",
                table: "Assessments");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Marks");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Assessments");

            migrationBuilder.CreateIndex(
                name: "IX_Marks_AssessmentId",
                table: "Marks",
                column: "AssessmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Marks_Assessments_AssessmentId",
                table: "Marks",
                column: "AssessmentId",
                principalTable: "Assessments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
