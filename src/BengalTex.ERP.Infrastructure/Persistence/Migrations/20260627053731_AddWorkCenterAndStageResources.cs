using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkCenterAndStageResources : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ShiftId",
                table: "ProductionStages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkCenterId",
                table: "ProductionStages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WorkCenters",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CapacityPerDay = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    CostPerHour = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_WorkCenters", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStages_ShiftId",
                table: "ProductionStages",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionStages_WorkCenterId",
                table: "ProductionStages",
                column: "WorkCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_Code",
                table: "WorkCenters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkCenters_IsActive",
                table: "WorkCenters",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionStages_Shifts_ShiftId",
                table: "ProductionStages",
                column: "ShiftId",
                principalTable: "Shifts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductionStages_WorkCenters_WorkCenterId",
                table: "ProductionStages",
                column: "WorkCenterId",
                principalTable: "WorkCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductionStages_Shifts_ShiftId",
                table: "ProductionStages");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductionStages_WorkCenters_WorkCenterId",
                table: "ProductionStages");

            migrationBuilder.DropTable(
                name: "WorkCenters");

            migrationBuilder.DropIndex(
                name: "IX_ProductionStages_ShiftId",
                table: "ProductionStages");

            migrationBuilder.DropIndex(
                name: "IX_ProductionStages_WorkCenterId",
                table: "ProductionStages");

            migrationBuilder.DropColumn(
                name: "ShiftId",
                table: "ProductionStages");

            migrationBuilder.DropColumn(
                name: "WorkCenterId",
                table: "ProductionStages");
        }
    }
}
