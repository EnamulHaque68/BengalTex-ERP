using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBangladeshPayroll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "FestivalBonus",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FoodAllowance",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseRent",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IncomeTax",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "LoanDeduction",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Medical",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PfEmployee",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PfEmployer",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Transport",
                table: "Payslips",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "FoodAllowance",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "HouseRentAllowance",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsPfMember",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTaxable",
                table: "Employees",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "MedicalAllowance",
                table: "Employees",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PfRate",
                table: "Employees",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TransportAllowance",
                table: "Employees",
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
                name: "FestivalBonus",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "FoodAllowance",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "HouseRent",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "IncomeTax",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "LoanDeduction",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "Medical",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "PfEmployee",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "PfEmployer",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "Transport",
                table: "Payslips");

            migrationBuilder.DropColumn(
                name: "FoodAllowance",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "HouseRentAllowance",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsPfMember",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "IsTaxable",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "MedicalAllowance",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "PfRate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TransportAllowance",
                table: "Employees");
        }
    }
}
