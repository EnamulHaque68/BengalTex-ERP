using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderLineInvoicedQty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedQuantity",
                table: "SalesOrderLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<long>(
                name: "SalesOrderLineId",
                table: "CustomerInvoiceLines",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoiceLines_SalesOrderLineId",
                table: "CustomerInvoiceLines",
                column: "SalesOrderLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerInvoiceLines_SalesOrderLines_SalesOrderLineId",
                table: "CustomerInvoiceLines",
                column: "SalesOrderLineId",
                principalTable: "SalesOrderLines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerInvoiceLines_SalesOrderLines_SalesOrderLineId",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoiceLines_SalesOrderLineId",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropColumn(
                name: "InvoicedQuantity",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "SalesOrderLineId",
                table: "CustomerInvoiceLines");
        }
    }
}
