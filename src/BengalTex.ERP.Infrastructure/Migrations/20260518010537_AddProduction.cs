using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_StockOnHand_RawMaterialWarehouse",
                table: "StockOnHand");

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "StockOnHand",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "StockOnHand",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "StockMovements",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ProductId",
                table: "StockMovements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductionOrders",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    BomId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    IssueWarehouseId = table.Column<int>(type: "int", nullable: false),
                    ReceiveWarehouseId = table.Column<int>(type: "int", nullable: false),
                    PlannedStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    PlannedEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ActualEndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CompletedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ModifiedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Boms_BomId",
                        column: x => x.BomId,
                        principalTable: "Boms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Warehouses_IssueWarehouseId",
                        column: x => x.IssueWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProductionOrders_Warehouses_ReceiveWarehouseId",
                        column: x => x.ReceiveWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_StockOnHand_ProductWarehouse",
                table: "StockOnHand",
                columns: new[] { "ProductId", "WarehouseId" },
                unique: true,
                filter: "[ProductId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_StockOnHand_RawMaterialWarehouse",
                table: "StockOnHand",
                columns: new[] { "RawMaterialId", "WarehouseId" },
                unique: true,
                filter: "[RawMaterialId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockOnHand_OneItemType",
                table: "StockOnHand",
                sql: "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId_WarehouseId",
                table: "StockMovements",
                columns: new[] { "ProductId", "WarehouseId" });

            migrationBuilder.AddCheckConstraint(
                name: "CK_StockMovement_OneItemType",
                table: "StockMovements",
                sql: "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_BomId",
                table: "ProductionOrders",
                column: "BomId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_Code",
                table: "ProductionOrders",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_IssueWarehouseId",
                table: "ProductionOrders",
                column: "IssueWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_PlannedStartDate",
                table: "ProductionOrders",
                column: "PlannedStartDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ProductId",
                table: "ProductionOrders",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_ReceiveWarehouseId",
                table: "ProductionOrders",
                column: "ReceiveWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_Status",
                table: "ProductionOrders",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockOnHand_Products_ProductId",
                table: "StockOnHand",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Products_ProductId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockOnHand_Products_ProductId",
                table: "StockOnHand");

            migrationBuilder.DropTable(
                name: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "UX_StockOnHand_ProductWarehouse",
                table: "StockOnHand");

            migrationBuilder.DropIndex(
                name: "UX_StockOnHand_RawMaterialWarehouse",
                table: "StockOnHand");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockOnHand_OneItemType",
                table: "StockOnHand");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductId_WarehouseId",
                table: "StockMovements");

            migrationBuilder.DropCheckConstraint(
                name: "CK_StockMovement_OneItemType",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "StockOnHand");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "StockMovements");

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "StockOnHand",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "StockMovements",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_StockOnHand_RawMaterialWarehouse",
                table: "StockOnHand",
                columns: new[] { "RawMaterialId", "WarehouseId" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
