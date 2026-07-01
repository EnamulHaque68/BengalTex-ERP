using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGoodsReceiptLcLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LetterOfCreditId",
                table: "GoodsReceiptNotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptNotes_LetterOfCreditId",
                table: "GoodsReceiptNotes",
                column: "LetterOfCreditId");

            migrationBuilder.AddForeignKey(
                name: "FK_GoodsReceiptNotes_LettersOfCredit_LetterOfCreditId",
                table: "GoodsReceiptNotes",
                column: "LetterOfCreditId",
                principalTable: "LettersOfCredit",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_GoodsReceiptNotes_LettersOfCredit_LetterOfCreditId",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptNotes_LetterOfCreditId",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropColumn(
                name: "LetterOfCreditId",
                table: "GoodsReceiptNotes");
        }
    }
}
