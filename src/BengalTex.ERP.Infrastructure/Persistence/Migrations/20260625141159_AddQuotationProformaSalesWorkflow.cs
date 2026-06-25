using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationProformaSalesWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "SalesOrders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConvertedProformaInvoiceId",
                table: "Quotations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConvertedSalesOrderId",
                table: "ProformaInvoices",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerConfirmationAttachment",
                table: "ProformaInvoices",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "CustomerConfirmationDate",
                table: "ProformaInvoices",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerConfirmationReference",
                table: "ProformaInvoices",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerConfirmationType",
                table: "ProformaInvoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuotationId",
                table: "ProformaInvoices",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProformaInvoices_QuotationId",
                table: "ProformaInvoices",
                column: "QuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProformaInvoices_Quotations_QuotationId",
                table: "ProformaInvoices",
                column: "QuotationId",
                principalTable: "Quotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProformaInvoices_Quotations_QuotationId",
                table: "ProformaInvoices");

            migrationBuilder.DropIndex(
                name: "IX_ProformaInvoices_QuotationId",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ConvertedProformaInvoiceId",
                table: "Quotations");

            migrationBuilder.DropColumn(
                name: "ConvertedSalesOrderId",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmationAttachment",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmationDate",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmationReference",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "CustomerConfirmationType",
                table: "ProformaInvoices");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "ProformaInvoices");
        }
    }
}
