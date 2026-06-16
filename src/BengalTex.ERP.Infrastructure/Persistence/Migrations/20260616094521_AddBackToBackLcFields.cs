using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackToBackLcFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MasterLcBuyer",
                table: "LettersOfCredit",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MasterLcReference",
                table: "LettersOfCredit",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "LettersOfCredit",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Import");

            migrationBuilder.CreateIndex(
                name: "IX_LettersOfCredit_Type",
                table: "LettersOfCredit",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LettersOfCredit_Type",
                table: "LettersOfCredit");

            migrationBuilder.DropColumn(
                name: "MasterLcBuyer",
                table: "LettersOfCredit");

            migrationBuilder.DropColumn(
                name: "MasterLcReference",
                table: "LettersOfCredit");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LettersOfCredit");
        }
    }
}
