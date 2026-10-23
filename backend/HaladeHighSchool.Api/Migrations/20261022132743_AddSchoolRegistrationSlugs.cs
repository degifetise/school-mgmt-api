using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolRegistrationSlugs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_StudentRegistrationRequests_PendingContactEmail",
                table: "StudentRegistrationRequests");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "StudentRegistrationRequests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "character varying(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Schools"
                SET "Slug" =
                    left(
                        coalesce(
                            nullif(trim(both '-' from regexp_replace(lower("Name"), '[^a-z0-9]+', '-', 'g')), ''),
                            'school'),
                        160)
                    || '-' || replace("Id"::text, '-', '');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "Schools",
                type: "character varying(220)",
                maxLength: 220,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(220)",
                oldMaxLength: 220,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolId",
                table: "StudentRegistrationRequests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.CreateIndex(
                name: "IX_StudentRegistrationRequests_School_Status_SubmittedAt",
                table: "StudentRegistrationRequests",
                columns: new[] { "SchoolId", "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "UQ_StudentRegistrationRequests_School_PendingContactEmail",
                table: "StudentRegistrationRequests",
                columns: new[] { "SchoolId", "ContactEmail" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "UQ_Schools_Slug",
                table: "Schools",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_StudentRegistrationRequests_Schools_SchoolId",
                table: "StudentRegistrationRequests",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentRegistrationRequests_Schools_SchoolId",
                table: "StudentRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "IX_StudentRegistrationRequests_School_Status_SubmittedAt",
                table: "StudentRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "UQ_StudentRegistrationRequests_School_PendingContactEmail",
                table: "StudentRegistrationRequests");

            migrationBuilder.DropIndex(
                name: "UQ_Schools_Slug",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "StudentRegistrationRequests");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Schools");

            migrationBuilder.CreateIndex(
                name: "UQ_StudentRegistrationRequests_PendingContactEmail",
                table: "StudentRegistrationRequests",
                column: "ContactEmail",
                unique: true,
                filter: "\"Status\" = 'Pending'");
        }
    }
}
