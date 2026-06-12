using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnNoteLinkOnCreditDebitNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SupplierReturnNoteId",
                table: "DebitNotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CustomerReturnNoteId",
                table: "CreditNotes",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DebitNotes_SupplierReturnNoteId",
                table: "DebitNotes",
                column: "SupplierReturnNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_CustomerReturnNoteId",
                table: "CreditNotes",
                column: "CustomerReturnNoteId");

            migrationBuilder.AddForeignKey(
                name: "FK_CreditNotes_CustomerReturnNotes_CustomerReturnNoteId",
                table: "CreditNotes",
                column: "CustomerReturnNoteId",
                principalTable: "CustomerReturnNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DebitNotes_SupplierReturnNotes_SupplierReturnNoteId",
                table: "DebitNotes",
                column: "SupplierReturnNoteId",
                principalTable: "SupplierReturnNotes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CreditNotes_CustomerReturnNotes_CustomerReturnNoteId",
                table: "CreditNotes");

            migrationBuilder.DropForeignKey(
                name: "FK_DebitNotes_SupplierReturnNotes_SupplierReturnNoteId",
                table: "DebitNotes");

            migrationBuilder.DropIndex(
                name: "IX_DebitNotes_SupplierReturnNoteId",
                table: "DebitNotes");

            migrationBuilder.DropIndex(
                name: "IX_CreditNotes_CustomerReturnNoteId",
                table: "CreditNotes");

            migrationBuilder.DropColumn(
                name: "SupplierReturnNoteId",
                table: "DebitNotes");

            migrationBuilder.DropColumn(
                name: "CustomerReturnNoteId",
                table: "CreditNotes");
        }
    }
}
