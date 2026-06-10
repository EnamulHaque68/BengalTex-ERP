using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddExportShippingDocumentFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CountryOfDestination",
                table: "CustomerInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeightKg",
                table: "CustomerInvoices",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IncoTerm",
                table: "CustomerInvoices",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWeightKg",
                table: "CustomerInvoices",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortOfDischarge",
                table: "CustomerInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortOfLoading",
                table: "CustomerInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingMarks",
                table: "CustomerInvoices",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalPackages",
                table: "CustomerInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VesselName",
                table: "CustomerInvoices",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryOfDestination",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "GrossWeightKg",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "IncoTerm",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "NetWeightKg",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "PortOfDischarge",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "PortOfLoading",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "ShippingMarks",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "TotalPackages",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "VesselName",
                table: "CustomerInvoices");
        }
    }
}
