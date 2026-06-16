using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryDispatchDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DriverName",
                table: "DeliveryNotes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "PlannedDeliveryDate",
                table: "DeliveryNotes",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransportCompany",
                table: "DeliveryNotes",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DriverName",
                table: "DeliveryNotes");

            migrationBuilder.DropColumn(
                name: "PlannedDeliveryDate",
                table: "DeliveryNotes");

            migrationBuilder.DropColumn(
                name: "TransportCompany",
                table: "DeliveryNotes");
        }
    }
}
