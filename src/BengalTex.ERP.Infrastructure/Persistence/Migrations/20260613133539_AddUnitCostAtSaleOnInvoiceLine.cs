using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUnitCostAtSaleOnInvoiceLine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "UnitCostAtSale",
                table: "CustomerInvoiceLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnitCostAtSale",
                table: "CustomerInvoiceLines");
        }
    }
}
