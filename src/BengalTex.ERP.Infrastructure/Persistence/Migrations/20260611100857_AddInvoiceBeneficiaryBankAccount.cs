using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBeneficiaryBankAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BeneficiaryBankAccountId",
                table: "CustomerInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_BeneficiaryBankAccountId",
                table: "CustomerInvoices",
                column: "BeneficiaryBankAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerInvoices_BankAccounts_BeneficiaryBankAccountId",
                table: "CustomerInvoices",
                column: "BeneficiaryBankAccountId",
                principalTable: "BankAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerInvoices_BankAccounts_BeneficiaryBankAccountId",
                table: "CustomerInvoices");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoices_BeneficiaryBankAccountId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "BeneficiaryBankAccountId",
                table: "CustomerInvoices");
        }
    }
}
