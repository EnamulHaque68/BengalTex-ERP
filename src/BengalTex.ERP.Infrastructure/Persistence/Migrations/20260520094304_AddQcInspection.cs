using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQcInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QcInspections",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SourceType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GoodsReceiptNoteId = table.Column<long>(type: "bigint", nullable: true),
                    ProductionOrderId = table.Column<long>(type: "bigint", nullable: true),
                    InspectionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InspectedFromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    QuarantineWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OverallResult = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    InspectedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_QcInspections", x => x.Id);
                    table.CheckConstraint("CK_QcInspection_OneSource", "([GoodsReceiptNoteId] IS NOT NULL AND [ProductionOrderId] IS NULL) OR ([GoodsReceiptNoteId] IS NULL AND [ProductionOrderId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QcInspections_GoodsReceiptNotes_GoodsReceiptNoteId",
                        column: x => x.GoodsReceiptNoteId,
                        principalTable: "GoodsReceiptNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcInspections_ProductionOrders_ProductionOrderId",
                        column: x => x.ProductionOrderId,
                        principalTable: "ProductionOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcInspections_Warehouses_InspectedFromWarehouseId",
                        column: x => x.InspectedFromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcInspections_Warehouses_QuarantineWarehouseId",
                        column: x => x.QuarantineWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QcInspectionLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QcInspectionId = table.Column<long>(type: "bigint", nullable: false),
                    RawMaterialId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    InspectedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    PassedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    RejectedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    DefectNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_QcInspectionLines", x => x.Id);
                    table.CheckConstraint("CK_QcInspectionLine_OneItemType", "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QcInspectionLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QcInspectionLines_QcInspections_QcInspectionId",
                        column: x => x.QcInspectionId,
                        principalTable: "QcInspections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QcInspectionLines_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QcInspectionLines_ProductId",
                table: "QcInspectionLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspectionLines_QcInspectionId",
                table: "QcInspectionLines",
                column: "QcInspectionId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspectionLines_RawMaterialId",
                table: "QcInspectionLines",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_Code",
                table: "QcInspections",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_GoodsReceiptNoteId",
                table: "QcInspections",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_InspectedFromWarehouseId",
                table: "QcInspections",
                column: "InspectedFromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_InspectionDate",
                table: "QcInspections",
                column: "InspectionDate");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_ProductionOrderId",
                table: "QcInspections",
                column: "ProductionOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_QuarantineWarehouseId",
                table: "QcInspections",
                column: "QuarantineWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QcInspections_Status",
                table: "QcInspections",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QcInspectionLines");

            migrationBuilder.DropTable(
                name: "QcInspections");
        }
    }
}
