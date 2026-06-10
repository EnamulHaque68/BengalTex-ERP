using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceLinePackingBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CartonNumberFrom",
                table: "CustomerInvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CartonNumberTo",
                table: "CustomerInvoiceLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GrossWeightKgPerLine",
                table: "CustomerInvoiceLines",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NetWeightKgPerLine",
                table: "CustomerInvoiceLines",
                type: "decimal(12,3)",
                precision: 12,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitsPerCarton",
                table: "CustomerInvoiceLines",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CartonNumberFrom",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropColumn(
                name: "CartonNumberTo",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropColumn(
                name: "GrossWeightKgPerLine",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropColumn(
                name: "NetWeightKgPerLine",
                table: "CustomerInvoiceLines");

            migrationBuilder.DropColumn(
                name: "UnitsPerCarton",
                table: "CustomerInvoiceLines");
        }
    }
}
