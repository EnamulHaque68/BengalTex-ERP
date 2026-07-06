using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryTruth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ClearsGrIr",
                table: "SupplierReturnNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "SupplierInvoiceLines",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AccountId",
                table: "SupplierInvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnCredit",
                table: "LandedCostVouchers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SettledAt",
                table: "LandedCostVouchers",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettledBy",
                table: "LandedCostVouchers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "SettledDate",
                table: "LandedCostVouchers",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SettlementMethod",
                table: "LandedCostVouchers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SupplierId",
                table: "LandedCostVouchers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGlPosted",
                table: "GoodsReceiptNotes",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierInvoiceLines_AccountId",
                table: "SupplierInvoiceLines",
                column: "AccountId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SupplierInvoiceLine_ItemXorService",
                table: "SupplierInvoiceLines",
                sql: "([RawMaterialId] IS NOT NULL AND [AccountId] IS NULL) OR ([RawMaterialId] IS NULL AND [AccountId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_LandedCostVouchers_SupplierId",
                table: "LandedCostVouchers",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsReceiptNotes_IsGlPosted",
                table: "GoodsReceiptNotes",
                column: "IsGlPosted");

            migrationBuilder.AddForeignKey(
                name: "FK_LandedCostVouchers_Suppliers_SupplierId",
                table: "LandedCostVouchers",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SupplierInvoiceLines_Accounts_AccountId",
                table: "SupplierInvoiceLines",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LandedCostVouchers_Suppliers_SupplierId",
                table: "LandedCostVouchers");

            migrationBuilder.DropForeignKey(
                name: "FK_SupplierInvoiceLines_Accounts_AccountId",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_SupplierInvoiceLines_AccountId",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SupplierInvoiceLine_ItemXorService",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropIndex(
                name: "IX_LandedCostVouchers_SupplierId",
                table: "LandedCostVouchers");

            migrationBuilder.DropIndex(
                name: "IX_GoodsReceiptNotes_IsGlPosted",
                table: "GoodsReceiptNotes");

            migrationBuilder.DropColumn(
                name: "ClearsGrIr",
                table: "SupplierReturnNotes");

            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "SupplierInvoiceLines");

            migrationBuilder.DropColumn(
                name: "IsOnCredit",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "SettledAt",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "SettledBy",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "SettledDate",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "SettlementMethod",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "SupplierId",
                table: "LandedCostVouchers");

            migrationBuilder.DropColumn(
                name: "IsGlPosted",
                table: "GoodsReceiptNotes");

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "SupplierInvoiceLines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
