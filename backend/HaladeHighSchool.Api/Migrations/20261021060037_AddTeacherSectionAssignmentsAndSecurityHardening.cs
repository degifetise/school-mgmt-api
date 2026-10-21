using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherSectionAssignmentsAndSecurityHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                UPDATE "RefreshTokens"
                SET "Token" = encode(digest(convert_to("Token", 'UTF8'), 'sha256'), 'hex'),
                    "ReplacedByToken" = CASE
                        WHEN "ReplacedByToken" IS NULL THEN NULL
                        ELSE encode(digest(convert_to("ReplacedByToken", 'UTF8'), 'sha256'), 'hex')
                    END;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<string>(
                name: "ReplacedByToken",
                table: "RefreshTokens",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "TeacherSectionAssignments",
                columns: table => new
                {
                    TeacherId = table.Column<int>(type: "integer", nullable: false),
                    GradeLevelId = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<int>(type: "integer", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherSectionAssignments", x => new { x.TeacherId, x.GradeLevelId, x.SectionId });
                    table.ForeignKey(
                        name: "FK_TeacherSectionAssignments_GradeLevelSections_GradeLevelId_S~",
                        columns: x => new { x.GradeLevelId, x.SectionId },
                        principalTable: "GradeLevelSections",
                        principalColumns: new[] { "GradeLevelId", "SectionId" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherSectionAssignments_Teachers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teachers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_SmartIDScanLogs_ScanStatus",
                table: "SmartIDScanLogs",
                sql: "\"ScanStatus\" IN ('VALID', 'EXPIRED', 'SUSPENDED', 'LOST', 'INVALID', 'NOT_FOUND', 'UID_ONLY')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SmartCards_CardType",
                table: "SmartCards",
                sql: "\"CardType\" IN ('STUDENT', 'STAFF', 'TEACHER')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SmartCards_Status",
                table: "SmartCards",
                sql: "\"Status\" IN ('ACTIVE', 'SUSPENDED', 'LOST', 'EXPIRED')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RefreshTokens_Expiry",
                table: "RefreshTokens",
                sql: "\"ExpiresAt\" > \"CreatedAt\"");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Marks_Score_NonNegative",
                table: "Marks",
                sql: "\"Score\" >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_AssessmentTypes_WeightPercentage_Range",
                table: "AssessmentTypes",
                sql: "\"WeightPercentage\" >= 0 AND \"WeightPercentage\" <= 100");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Assessments_MaxScore_Positive",
                table: "Assessments",
                sql: "\"MaxScore\" > 0");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSectionAssignments_GradeLevelId_SectionId",
                table: "TeacherSectionAssignments",
                columns: new[] { "GradeLevelId", "SectionId" });

            migrationBuilder.CreateIndex(
                name: "IX_TeacherSectionAssignments_Teacher_Section",
                table: "TeacherSectionAssignments",
                columns: new[] { "TeacherId", "SectionId" });

            migrationBuilder.Sql("""
                INSERT INTO "TeacherSectionAssignments" ("TeacherId", "GradeLevelId", "SectionId")
                SELECT DISTINCT assignment."TeacherId", subject."GradeLevelId", assignment."SectionId"
                FROM "TeacherSubjects" AS assignment
                INNER JOIN "Subjects" AS subject ON subject."Id" = assignment."SubjectId"
                INNER JOIN "GradeLevelSections" AS grade_section
                    ON grade_section."GradeLevelId" = subject."GradeLevelId"
                    AND grade_section."SectionId" = assignment."SectionId"
                WHERE assignment."IsActive" = TRUE
                ON CONFLICT ("TeacherId", "GradeLevelId", "SectionId") DO NOTHING;

                INSERT INTO "AssessmentTypes" ("Name", "DisplayName", "WeightPercentage", "DisplayOrder", "IsActive")
                VALUES
                    ('Quiz', 'Quiz', 10.00, 1, TRUE),
                    ('Test', 'Test', 10.00, 2, TRUE),
                    ('MidExam', 'Midterm Examination', 30.00, 3, TRUE),
                    ('FinalExam', 'Final Examination', 50.00, 4, TRUE),
                    ('Assignment', 'Assignment', 0.01, 99, FALSE),
                    ('Other', 'Other', 0.01, 100, FALSE)
                ON CONFLICT ("Name") DO NOTHING;

                CREATE OR REPLACE VIEW "vw_StudentSubjectPerformance" AS
                WITH type_aggregate AS
                (
                    SELECT mark."StudentId",
                           mark."SubjectId",
                           assessment."AssessmentType",
                           SUM(mark."Score") AS earned,
                           SUM(assessment."MaxScore") AS possible
                    FROM "Marks" AS mark
                    INNER JOIN "Assessments" AS assessment ON assessment."Id" = mark."AssessmentId"
                    WHERE mark."IsPublished" = TRUE
                    GROUP BY mark."StudentId", mark."SubjectId", assessment."AssessmentType"
                ),
                weighted AS
                (
                    SELECT type_scores."StudentId",
                           type_scores."SubjectId",
                           type_scores."AssessmentType",
                           ROUND(
                               type_scores.earned / NULLIF(type_scores.possible, 0)
                               * assessment_type."WeightPercentage",
                               2
                           )::numeric(6, 2) AS weighted_score
                    FROM type_aggregate AS type_scores
                    INNER JOIN "AssessmentTypes" AS assessment_type
                        ON assessment_type."Name" = type_scores."AssessmentType"
                    WHERE assessment_type."IsActive" = TRUE
                ),
                totals AS
                (
                    SELECT weighted."StudentId",
                           weighted."SubjectId",
                           SUM(weighted.weighted_score) AS total_score,
                           SUM(weighted.weighted_score) FILTER (WHERE weighted."AssessmentType" = 'Quiz') AS quiz_score,
                           SUM(weighted.weighted_score) FILTER (WHERE weighted."AssessmentType" = 'Test') AS test_score,
                           SUM(weighted.weighted_score) FILTER (WHERE weighted."AssessmentType" = 'MidExam') AS mid_exam_score,
                           SUM(weighted.weighted_score) FILTER (WHERE weighted."AssessmentType" = 'FinalExam') AS final_exam_score
                    FROM weighted
                    GROUP BY weighted."StudentId", weighted."SubjectId"
                )
                SELECT totals."StudentId",
                       totals."SubjectId",
                       totals.quiz_score::numeric(6, 2) AS "QuizScore",
                       totals.test_score::numeric(6, 2) AS "TestScore",
                       totals.mid_exam_score::numeric(6, 2) AS "MidExamScore",
                       totals.final_exam_score::numeric(6, 2) AS "FinalExamScore",
                       COALESCE(totals.total_score, 0)::numeric(6, 2) AS "TotalScore",
                       COALESCE(totals.total_score, 0) >= 50 AS "IsPassed",
                       CASE WHEN COALESCE(totals.total_score, 0) >= 50 THEN 'Pass' ELSE 'Fail' END AS "Status"
                FROM totals;

                CREATE OR REPLACE FUNCTION validate_mark_score()
                RETURNS trigger
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    assessment_max_score numeric(6, 2);
                BEGIN
                    SELECT "MaxScore"
                    INTO assessment_max_score
                    FROM "Assessments"
                    WHERE "Id" = NEW."AssessmentId";

                    IF assessment_max_score IS NULL
                       OR NEW."Score" < 0
                       OR NEW."Score" > assessment_max_score THEN
                        RAISE EXCEPTION 'Mark score must be between zero and the assessment maximum'
                            USING ERRCODE = '23514';
                    END IF;

                    RETURN NEW;
                END;
                $$;

                CREATE TRIGGER "TR_Marks_Validate"
                BEFORE INSERT OR UPDATE OF "AssessmentId", "Score"
                ON "Marks"
                FOR EACH ROW
                EXECUTE FUNCTION validate_mark_score();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP VIEW IF EXISTS "vw_StudentSubjectPerformance";
                DROP TRIGGER IF EXISTS "TR_Marks_Validate" ON "Marks";
                DROP FUNCTION IF EXISTS validate_mark_score();
                """);

            migrationBuilder.DropTable(
                name: "TeacherSectionAssignments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SmartIDScanLogs_ScanStatus",
                table: "SmartIDScanLogs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SmartCards_CardType",
                table: "SmartCards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SmartCards_Status",
                table: "SmartCards");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RefreshTokens_Expiry",
                table: "RefreshTokens");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Marks_Score_NonNegative",
                table: "Marks");

            migrationBuilder.DropCheckConstraint(
                name: "CK_AssessmentTypes_WeightPercentage_Range",
                table: "AssessmentTypes");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Assessments_MaxScore_Positive",
                table: "Assessments");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "RefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);

            migrationBuilder.AlterColumn<string>(
                name: "ReplacedByToken",
                table: "RefreshTokens",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
