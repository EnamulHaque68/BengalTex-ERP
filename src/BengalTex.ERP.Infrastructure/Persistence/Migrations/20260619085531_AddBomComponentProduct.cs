using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBomComponentProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "BomLines",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "ComponentProductId",
                table: "BomLines",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_BomLines_ComponentProductId",
                table: "BomLines",
                column: "ComponentProductId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_BomLine_OneItem",
                table: "BomLines",
                sql: "([RawMaterialId] IS NOT NULL AND [ComponentProductId] IS NULL) OR ([RawMaterialId] IS NULL AND [ComponentProductId] IS NOT NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines",
                column: "ComponentProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BomLines_Products_ComponentProductId",
                table: "BomLines");

            migrationBuilder.DropIndex(
                name: "IX_BomLines_ComponentProductId",
                table: "BomLines");

            migrationBuilder.DropCheckConstraint(
                name: "CK_BomLine_OneItem",
                table: "BomLines");

            migrationBuilder.DropColumn(
                name: "ComponentProductId",
                table: "BomLines");

            migrationBuilder.AlterColumn<int>(
                name: "RawMaterialId",
                table: "BomLines",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
