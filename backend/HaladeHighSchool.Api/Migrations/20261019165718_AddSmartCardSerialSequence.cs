using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HaladeHighSchool.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSmartCardSerialSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence(
                name: "SmartCardSerialSequence");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "SmartCardSerialSequence");
        }
    }
}
