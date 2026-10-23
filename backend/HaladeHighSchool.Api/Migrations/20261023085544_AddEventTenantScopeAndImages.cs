using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEventTenantScopeAndImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string[]>(
                name: "ImageUrls",
                table: "Events",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<Guid>(
                name: "SchoolId",
                table: "Events",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE "Events" AS ev
                SET "SchoolId" = account."SchoolId"
                FROM "AspNetUsers" AS account
                WHERE ev."OrganizedByUserId" = account."Id"
                  AND ev."SchoolId" IS NULL;

                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "Events" WHERE "SchoolId" IS NULL) THEN
                        RAISE EXCEPTION 'Event tenant migration blocked: assign every event without a valid organizer to a school before retrying.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "SchoolId",
                table: "Events",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Events_SchoolId",
                table: "Events",
                column: "SchoolId");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Schools_SchoolId",
                table: "Events",
                column: "SchoolId",
                principalTable: "Schools",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Events_Schools_SchoolId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_Events_SchoolId",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SchoolId",
                table: "Events");
        }
    }
}
