using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrderSourceLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "PurchaseRequisitionId",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SupplierQuotationId",
                table: "PurchaseOrders",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_PurchaseRequisitionId",
                table: "PurchaseOrders",
                column: "PurchaseRequisitionId",
                unique: true,
                filter: "[PurchaseRequisitionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierQuotationId",
                table: "PurchaseOrders",
                column: "SupplierQuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_PurchaseRequisitions_PurchaseRequisitionId",
                table: "PurchaseOrders",
                column: "PurchaseRequisitionId",
                principalTable: "PurchaseRequisitions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PurchaseOrders_SupplierQuotations_SupplierQuotationId",
                table: "PurchaseOrders",
                column: "SupplierQuotationId",
                principalTable: "SupplierQuotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_PurchaseRequisitions_PurchaseRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_PurchaseOrders_SupplierQuotations_SupplierQuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_PurchaseRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_PurchaseOrders_SupplierQuotationId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "PurchaseRequisitionId",
                table: "PurchaseOrders");

            migrationBuilder.DropColumn(
                name: "SupplierQuotationId",
                table: "PurchaseOrders");
        }
    }
}
