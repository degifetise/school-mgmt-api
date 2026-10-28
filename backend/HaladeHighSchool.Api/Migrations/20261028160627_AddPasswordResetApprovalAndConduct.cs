using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetApprovalAndConduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "PasswordResetRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "PasswordResetRequests",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "PasswordResetRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "IsApproved",
                table: "PasswordResetRequests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedDate",
                table: "PasswordResetRequests",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestDate",
                table: "PasswordResetRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "CURRENT_TIMESTAMP");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "PasswordResetRequests",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PasswordResetRequests",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<bool>(
                name: "MustChangePasswordOnLogin",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "StudentConductLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentId = table.Column<int>(type: "integer", nullable: false),
                    RecordedByUserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    RecordedByName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    RecordedByRole = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Title = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    IncidentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVisibleToStudent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsVisibleToParent = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentConductLogs", x => x.Id);
                    table.CheckConstraint("CK_StudentConductLogs_Type", "\"Type\" IN ('Merit', 'Demerit', 'Disciplinary Note', 'General Behavioral Log')");
                    table.ForeignKey(
                        name: "FK_StudentConductLogs_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetRequests_Status_RequestDate",
                table: "PasswordResetRequests",
                columns: new[] { "Status", "RequestDate" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentConductLogs_RecordedByUserId",
                table: "StudentConductLogs",
                column: "RecordedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentConductLogs_Student_IncidentDate",
                table: "StudentConductLogs",
                columns: new[] { "StudentId", "IncidentDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentConductLogs");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetRequests_Status_RequestDate",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "IsApproved",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "ProcessedDate",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "RequestDate",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PasswordResetRequests");

            migrationBuilder.DropColumn(
                name: "MustChangePasswordOnLogin",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "Token",
                table: "PasswordResetRequests",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ExpiresAt",
                table: "PasswordResetRequests",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
