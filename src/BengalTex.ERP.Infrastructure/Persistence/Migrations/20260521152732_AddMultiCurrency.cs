using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SupplierInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SupplierInvoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "SalesOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "SalesOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "PurchaseOrders",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "PurchaseOrders",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "CustomerInvoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "CustomerInvoices",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                defaultValue: 0m);

            // Backfill existing rows to base currency (BDT, rate = 1) BEFORE the FK is created.
            // New columns default to CurrencyId = 0 / ExchangeRate = 0, which would violate the FK
            // and the "rate > 0" semantics. On a DB with documents, BDT was already seeded.
            migrationBuilder.Sql(@"
                DECLARE @bdt INT = (SELECT TOP 1 Id FROM Currencies WHERE Code = 'BDT');
                IF @bdt IS NOT NULL
                BEGIN
                    UPDATE PurchaseOrders   SET CurrencyId = @bdt, ExchangeRate = 1 WHERE CurrencyId = 0;
                    UPDATE SalesOrders      SET CurrencyId = @bdt, ExchangeRate = 1 WHERE CurrencyId = 0;
                    UPDATE CustomerInvoices SET CurrencyId = @bdt, ExchangeRate = 1 WHERE CurrencyId = 0;
                    UPDATE SupplierInvoices SET CurrencyId = @bdt, ExchangeRate = 1 WHERE CurrencyId = 0;
                END");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoices_CurrencyId",
                table: "SupplierInvoices",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrders_CurrencyId",
                table: "SalesOrders",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_CurrencyId",
                table: "PurchaseOrders",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_CurrencyId",
                table: "CustomerInvoices",
                column: "CurrencyId");

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerInvoices_Currencies_CurrencyId",
                table: "CustomerInvoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_Currencies_CurrencyId",
                table: "PurchaseOrders",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrders_Currencies_CurrencyId",
                table: "SalesOrders",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoices_Currencies_CurrencyId",
                table: "SupplierInvoices",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerInvoices_Currencies_CurrencyId",
                table: "CustomerInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_Currencies_CurrencyId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrders_Currencies_CurrencyId",
                table: "SalesOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoices_Currencies_CurrencyId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoices_CurrencyId",
                table: "SupplierInvoices");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrders_CurrencyId",
                table: "SalesOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_CurrencyId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoices_CurrencyId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "SalesOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "CustomerInvoices");
        }
    }
}
