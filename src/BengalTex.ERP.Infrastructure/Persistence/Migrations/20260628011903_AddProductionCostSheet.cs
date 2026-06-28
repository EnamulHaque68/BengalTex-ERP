using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProductionCostSheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LabourCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MachineCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaterialCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "OverheadCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RejectCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubcontractCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "WastageCost",
                table: "ProductionOrders",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LabourCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "MachineCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "MaterialCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "OverheadCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "RejectCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "SubcontractCost",
                table: "ProductionOrders");

            migrationBuilder.DropColumn(
                name: "WastageCost",
                table: "ProductionOrders");
        }
    }
}
