using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceContainerSealTruck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContainerNumber",
                table: "CustomerInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SealNumber",
                table: "CustomerInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TruckNumber",
                table: "CustomerInvoices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContainerNumber",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "SealNumber",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "TruckNumber",
                table: "CustomerInvoices");
        }
    }
}
