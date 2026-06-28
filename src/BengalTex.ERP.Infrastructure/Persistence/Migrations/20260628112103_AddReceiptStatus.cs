using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PostedAt",
                table: "Receipts",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostedBy",
                table: "Receipts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            // Existing receipts were applied to their invoices under the old single-state model,
            // so they are effectively already Posted. Backfill them as "Posted" (NOT the scaffolded
            // empty string). New receipts are inserted with an explicit "Draft" value by EF, so this
            // column default only ever touches these pre-existing rows.
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Receipts",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Posted");

            migrationBuilder.CreateIndex(
                name: "IX_Receipts_Status",
                table: "Receipts",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Receipts_Status",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PostedAt",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "PostedBy",
                table: "Receipts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Receipts");
        }
    }
}
