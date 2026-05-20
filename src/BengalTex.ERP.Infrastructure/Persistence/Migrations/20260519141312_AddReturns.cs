using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReturns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "GoodsReceiptLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQuantity",
                table: "DeliveryNoteLines",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CustomerReturnNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DeliveryNoteId = table.Column<long>(type: "bigint", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_CustomerReturnNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnNotes_DeliveryNotes_DeliveryNoteId",
                        column: x => x.DeliveryNoteId,
                        principalTable: "DeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerReturnNotes_Warehouses_ReturnWarehouseId",
                        column: x => x.ReturnWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GoodsReceiptNoteId = table.Column<long>(type: "bigint", nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnFromWarehouseId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PostedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_SupplierReturnNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnNotes_GoodsReceiptNotes_GoodsReceiptNoteId",
                        column: x => x.GoodsReceiptNoteId,
                        principalTable: "GoodsReceiptNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnNotes_Warehouses_ReturnFromWarehouseId",
                        column: x => x.ReturnFromWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CustomerReturnNoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CustomerReturnNoteId = table.Column<long>(type: "bigint", nullable: false),
                    DeliveryNoteLineId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LineNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_CustomerReturnNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerReturnNoteLines_CustomerReturnNotes_CustomerReturnNoteId",
                        column: x => x.CustomerReturnNoteId,
                        principalTable: "CustomerReturnNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerReturnNoteLines_DeliveryNoteLines_DeliveryNoteLineId",
                        column: x => x.DeliveryNoteLineId,
                        principalTable: "DeliveryNoteLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CustomerReturnNoteLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SupplierReturnNoteLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierReturnNoteId = table.Column<long>(type: "bigint", nullable: false),
                    GoodsReceiptLineId = table.Column<long>(type: "bigint", nullable: false),
                    RawMaterialId = table.Column<int>(type: "int", nullable: false),
                    ReturnedQuantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    LineNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_SupplierReturnNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SupplierReturnNoteLines_GoodsReceiptLines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "GoodsReceiptLines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnNoteLines_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SupplierReturnNoteLines_SupplierReturnNotes_SupplierReturnNoteId",
                        column: x => x.SupplierReturnNoteId,
                        principalTable: "SupplierReturnNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNoteLines_CustomerReturnNoteId",
                table: "CustomerReturnNoteLines",
                column: "CustomerReturnNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNoteLines_DeliveryNoteLineId",
                table: "CustomerReturnNoteLines",
                column: "DeliveryNoteLineId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNoteLines_ProductId",
                table: "CustomerReturnNoteLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNotes_Code",
                table: "CustomerReturnNotes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNotes_DeliveryNoteId",
                table: "CustomerReturnNotes",
                column: "DeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNotes_ReturnDate",
                table: "CustomerReturnNotes",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNotes_ReturnWarehouseId",
                table: "CustomerReturnNotes",
                column: "ReturnWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerReturnNotes_Status",
                table: "CustomerReturnNotes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNoteLines_GoodsReceiptLineId",
                table: "SupplierReturnNoteLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNoteLines_RawMaterialId",
                table: "SupplierReturnNoteLines",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNoteLines_SupplierReturnNoteId",
                table: "SupplierReturnNoteLines",
                column: "SupplierReturnNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNotes_Code",
                table: "SupplierReturnNotes",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNotes_GoodsReceiptNoteId",
                table: "SupplierReturnNotes",
                column: "GoodsReceiptNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNotes_ReturnDate",
                table: "SupplierReturnNotes",
                column: "ReturnDate");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNotes_ReturnFromWarehouseId",
                table: "SupplierReturnNotes",
                column: "ReturnFromWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SupplierReturnNotes_Status",
                table: "SupplierReturnNotes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomerReturnNoteLines");

            migrationBuilder.DropTable(
                name: "SupplierReturnNoteLines");

            migrationBuilder.DropTable(
                name: "CustomerReturnNotes");

            migrationBuilder.DropTable(
                name: "SupplierReturnNotes");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "GoodsReceiptLines");

            migrationBuilder.DropColumn(
                name: "ReturnedQuantity",
                table: "DeliveryNoteLines");
        }
    }
}
