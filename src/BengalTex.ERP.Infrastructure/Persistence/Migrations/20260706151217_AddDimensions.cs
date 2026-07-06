using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDimensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "StyleId",
                table: "SalesOrderLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StyleId",
                table: "ProductionOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyerId",
                table: "JournalEntryLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostCenterId",
                table: "JournalEntryLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProductionOrderId",
                table: "JournalEntryLines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SalesOrderId",
                table: "JournalEntryLines",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StyleId",
                table: "JournalEntryLines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CostCenterId",
                table: "Expenses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresCostCenter",
                table: "Accounts",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "CostCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParentCostCenterId = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<int>(type: "int", nullable: true),
                    FactoryId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_CostCenters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CostCenters_CostCenters_ParentCostCenterId",
                        column: x => x.ParentCostCenterId,
                        principalTable: "CostCenters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CostCenters_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_CostCenters_Factories_FactoryId",
                        column: x => x.FactoryId,
                        principalTable: "Factories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalesOrderLines_StyleId",
                table: "SalesOrderLines",
                column: "StyleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionOrders_StyleId",
                table: "ProductionOrders",
                column: "StyleId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_BuyerId",
                table: "JournalEntryLines",
                column: "BuyerId",
                filter: "[BuyerId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_CostCenterId",
                table: "JournalEntryLines",
                column: "CostCenterId",
                filter: "[CostCenterId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_ProductionOrderId",
                table: "JournalEntryLines",
                column: "ProductionOrderId",
                filter: "[ProductionOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_SalesOrderId",
                table: "JournalEntryLines",
                column: "SalesOrderId",
                filter: "[SalesOrderId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_StyleId",
                table: "JournalEntryLines",
                column: "StyleId",
                filter: "[StyleId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CostCenterId",
                table: "Expenses",
                column: "CostCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_Code",
                table: "CostCenters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_DepartmentId",
                table: "CostCenters",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_FactoryId",
                table: "CostCenters",
                column: "FactoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_Kind",
                table: "CostCenters",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_CostCenters_ParentCostCenterId",
                table: "CostCenters",
                column: "ParentCostCenterId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_CostCenters_CostCenterId",
                table: "Expenses",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_CostCenters_CostCenterId",
                table: "JournalEntryLines",
                column: "CostCenterId",
                principalTable: "CostCenters",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_Customers_BuyerId",
                table: "JournalEntryLines",
                column: "BuyerId",
                principalTable: "Customers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_ProductionOrders_ProductionOrderId",
                table: "JournalEntryLines",
                column: "ProductionOrderId",
                principalTable: "ProductionOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_SalesOrders_SalesOrderId",
                table: "JournalEntryLines",
                column: "SalesOrderId",
                principalTable: "SalesOrders",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_JournalEntryLines_Styles_StyleId",
                table: "JournalEntryLines",
                column: "StyleId",
                principalTable: "Styles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionOrders_Styles_StyleId",
                table: "ProductionOrders",
                column: "StyleId",
                principalTable: "Styles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesOrderLines_Styles_StyleId",
                table: "SalesOrderLines",
                column: "StyleId",
                principalTable: "Styles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_CostCenters_CostCenterId",
                table: "Expenses");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_CostCenters_CostCenterId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_Customers_BuyerId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_ProductionOrders_ProductionOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_SalesOrders_SalesOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_JournalEntryLines_Styles_StyleId",
                table: "JournalEntryLines");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionOrders_Styles_StyleId",
                table: "ProductionOrders");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesOrderLines_Styles_StyleId",
                table: "SalesOrderLines");

            migrationBuilder.DropTable(
                name: "CostCenters");

            migrationBuilder.DropIndex(
                name: "IX_SalesOrderLines_StyleId",
                table: "SalesOrderLines");

            migrationBuilder.DropIndex(
                name: "IX_ProductionOrders_StyleId",
                table: "ProductionOrders");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_BuyerId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_CostCenterId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_ProductionOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_SalesOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_JournalEntryLines_StyleId",
                table: "JournalEntryLines");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_CostCenterId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "StyleId",
                table: "SalesOrderLines");

            migrationBuilder.DropColumn(
                name: "StyleId",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "BuyerId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "SalesOrderId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "StyleId",
                table: "JournalEntryLines");

            migrationBuilder.DropColumn(
                name: "CostCenterId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "RequiresCostCenter",
                table: "Accounts");
        }
    }
}
