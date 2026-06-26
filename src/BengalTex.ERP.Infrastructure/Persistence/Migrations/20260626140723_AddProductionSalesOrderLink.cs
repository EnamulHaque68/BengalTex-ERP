using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionSalesOrderLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SalesOrderId",
                table: "ProductionOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SalesOrderLineId",
                table: "ProductionOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SalesOrderId",
                table: "ProductionOrders",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_SalesOrderLineId",
                table: "ProductionOrders",
                column: "SalesOrderLineId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_SalesOrderLines_SalesOrderLineId",
                table: "ProductionOrders",
                column: "SalesOrderLineId",
                principalTable: "SalesOrderLines",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_SalesOrders_SalesOrderId",
                table: "ProductionOrders",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_SalesOrderLines_SalesOrderLineId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_SalesOrders_SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_SalesOrderLineId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SalesOrderLineId",
                table: "ProductionOrders");
        }
    }
}
