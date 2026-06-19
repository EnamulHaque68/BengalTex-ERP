using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerParent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ParentCustomerId",
                table: "Customers",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_ParentCustomerId",
                table: "Customers",
                column: "ParentCustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Customers_Customers_ParentCustomerId",
                table: "Customers",
                column: "ParentCustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Customers_Customers_ParentCustomerId",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Customers_ParentCustomerId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ParentCustomerId",
                table: "Customers");
        }
    }
}
