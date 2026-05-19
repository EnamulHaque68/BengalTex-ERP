using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVatToInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "SupplierInvoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "SupplierInvoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "SupplierInvoices",
                type: "decimal(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "SubtotalAmount",
                table: "CustomerInvoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatAmount",
                table: "CustomerInvoices",
                type: "decimal(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "VatRate",
                table: "CustomerInvoices",
                type: "decimal(7,4)",
                precision: 7,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "VatChallans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CustomerInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    ChallanDate = table.Column<DateOnly>(type: "date", nullable: false),
                    VatAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SubtotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
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
                    table.PrimaryKey("PK_VatChallans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VatChallans_CustomerInvoices_CustomerInvoiceId",
                        column: x => x.CustomerInvoiceId,
                        principalTable: "CustomerInvoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VatChallans_ChallanDate",
                table: "VatChallans",
                column: "ChallanDate");

            migrationBuilder.CreateIndex(
                name: "IX_VatChallans_Code",
                table: "VatChallans",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_VatChallans_CustomerInvoice",
                table: "VatChallans",
                column: "CustomerInvoiceId",
                unique: true,
                filter: "[IsDeleted] = 0");

            // ─── Backfill pre-Phase-12 invoices ─────────────────────────────────
            // Existing rows have TotalAmount = sum of line totals (pre-VAT semantics).
            // Set SubtotalAmount = TotalAmount so the new "Subtotal" matches the historic
            // sum-of-lines value. VatRate + VatAmount stay 0 (no retroactive VAT) and
            // TotalAmount stays unchanged (Subtotal + 0 VAT = original Total). The
            // invariant Total = Subtotal + VatAmount holds for both old and new rows.
            migrationBuilder.Sql("UPDATE CustomerInvoices SET SubtotalAmount = TotalAmount;");
            migrationBuilder.Sql("UPDATE SupplierInvoices SET SubtotalAmount = TotalAmount;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VatChallans");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "SupplierInvoices");

            migrationBuilder.DropColumn(
                name: "SubtotalAmount",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "VatAmount",
                table: "CustomerInvoices");

            migrationBuilder.DropColumn(
                name: "VatRate",
                table: "CustomerInvoices");
        }
    }
}
