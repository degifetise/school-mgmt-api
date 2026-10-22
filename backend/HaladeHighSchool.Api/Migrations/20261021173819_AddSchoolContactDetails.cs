using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchoolContactDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Schools",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Code",
                table: "Schools",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Schools",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Schools",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE "Schools"
                SET "Code" = 'DEFAULT'
                WHERE "Id" = '11111111-1111-1111-1111-111111111111'
                  AND "Code" = '';

                ALTER TABLE "Schools"
                    ALTER COLUMN "Address" DROP DEFAULT,
                    ALTER COLUMN "Code" DROP DEFAULT,
                    ALTER COLUMN "ContactEmail" DROP DEFAULT,
                    ALTER COLUMN "ContactPhone" DROP DEFAULT;
                """);

            migrationBuilder.CreateIndex(
                name: "UQ_Schools_Code",
                table: "Schools",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UQ_Schools_Code",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Address",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "Code",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Schools");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Schools");
        }
    }
}
