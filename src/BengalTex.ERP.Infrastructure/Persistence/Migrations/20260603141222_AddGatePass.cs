using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGatePass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GatePasses",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PassDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PassTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Direction = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    VehicleNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DriverName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    DriverPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    DriverNidNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    TransporterName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VisitorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    VisitorPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    VisitorOrganization = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    VisitorPurpose = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ItemDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Quantity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FromLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    ToLocation = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SourceType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SourceId = table.Column<long>(type: "bigint", nullable: true),
                    SourceCode = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ApprovedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ExpectedReturnDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ReturnedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ReturnedByUser = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReturnNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    table.PrimaryKey("PK_GatePasses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_Code",
                table: "GatePasses",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_PassDate",
                table: "GatePasses",
                column: "PassDate");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_SourceType_SourceId",
                table: "GatePasses",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_Status",
                table: "GatePasses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_Type",
                table: "GatePasses",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_GatePasses_VehicleNumber",
                table: "GatePasses",
                column: "VehicleNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GatePasses");
        }
    }
}
