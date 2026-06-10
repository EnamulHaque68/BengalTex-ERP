using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportReportingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HsCode",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EpbFormNumber",
                table: "CustomerInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LcNumber",
                table: "CustomerInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "ShipmentDate",
                table: "CustomerInvoices",
                type: "date",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_HsCode",
                table: "Products",
                column: "HsCode");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_EpbFormNumber",
                table: "CustomerInvoices",
                column: "EpbFormNumber");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerInvoices_ShipmentDate",
                table: "CustomerInvoices",
                column: "ShipmentDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_HsCode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoices_EpbFormNumber",
                table: "CustomerInvoices");

            migrationBuilder.DropIndex(
                name: "IX_CustomerInvoices_ShipmentDate",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "HsCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "EpbFormNumber",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "LcNumber",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "ShipmentDate",
                table: "CustomerInvoices");
        }
    }
}
