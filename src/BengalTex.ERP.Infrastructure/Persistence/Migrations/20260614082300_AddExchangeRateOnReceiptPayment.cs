using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExchangeRateOnReceiptPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Receipts",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Payments",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 1m);

            // Backfill existing rows from their parent invoice's locked rate (these legacy
            // receipts/payments posted their journals at the invoice rate, so no FX gain/loss
            // ever applied — set the snapshot to match for accurate display).
            migrationBuilder.Sql(
                "UPDATE r SET r.ExchangeRate = i.ExchangeRate " +
                "FROM Receipts r JOIN CustomerInvoices i ON i.Id = r.CustomerInvoiceId;");
            migrationBuilder.Sql(
                "UPDATE p SET p.ExchangeRate = i.ExchangeRate " +
                "FROM Payments p JOIN SupplierInvoices i ON i.Id = p.SupplierInvoiceId;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Payments");
        }
    }
}
