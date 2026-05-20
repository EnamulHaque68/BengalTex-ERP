using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQuarantineDisposition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "QuarantineDispositions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DispositionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DispositionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    QuarantineWarehouseId = table.Column<int>(type: "int", nullable: false),
                    DestinationWarehouseId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_QuarantineDispositions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QuarantineDispositions_Warehouses_DestinationWarehouseId",
                        column: x => x.DestinationWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuarantineDispositions_Warehouses_QuarantineWarehouseId",
                        column: x => x.QuarantineWarehouseId,
                        principalTable: "Warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "QuarantineDispositionLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    QuarantineDispositionId = table.Column<long>(type: "bigint", nullable: false),
                    RawMaterialId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_QuarantineDispositionLines", x => x.Id);
                    table.CheckConstraint("CK_QuarantineDispositionLine_OneItemType", "([RawMaterialId] IS NOT NULL AND [ProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ProductId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_QuarantineDispositionLines_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_QuarantineDispositionLines_QuarantineDispositions_QuarantineDispositionId",
                        column: x => x.QuarantineDispositionId,
                        principalTable: "QuarantineDispositions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_QuarantineDispositionLines_RawMaterials_RawMaterialId",
                        column: x => x.RawMaterialId,
                        principalTable: "RawMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositionLines_ProductId",
                table: "QuarantineDispositionLines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositionLines_QuarantineDispositionId",
                table: "QuarantineDispositionLines",
                column: "QuarantineDispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositionLines_RawMaterialId",
                table: "QuarantineDispositionLines",
                column: "RawMaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_Code",
                table: "QuarantineDispositions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_DestinationWarehouseId",
                table: "QuarantineDispositions",
                column: "DestinationWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_DispositionDate",
                table: "QuarantineDispositions",
                column: "DispositionDate");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_QuarantineWarehouseId",
                table: "QuarantineDispositions",
                column: "QuarantineWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_QuarantineDispositions_Status",
                table: "QuarantineDispositions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "QuarantineDispositionLines");

            migrationBuilder.DropTable(
                name: "QuarantineDispositions");
        }
    }
}
