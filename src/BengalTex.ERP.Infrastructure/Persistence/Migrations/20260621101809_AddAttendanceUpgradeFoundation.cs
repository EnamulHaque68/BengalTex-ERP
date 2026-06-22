using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceUpgradeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "AttendanceRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotRequired");   // valid enum name so existing rows materialise

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ApprovedAt",
                table: "AttendanceRecords",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedByEmployeeId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInSelfieUrl",
                table: "AttendanceRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckOutDistanceMeters",
                table: "AttendanceRecords",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckOutLatitude",
                table: "AttendanceRecords",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CheckOutLongitude",
                table: "AttendanceRecords",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckOutSelfieUrl",
                table: "AttendanceRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckOutWithinFence",
                table: "AttendanceRecords",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "FaceMatchScore",
                table: "AttendanceRecords",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FaceMatchStatus",
                table: "AttendanceRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NotChecked");   // valid enum name so existing rows materialise

            migrationBuilder.AddColumn<bool>(
                name: "IsEarlyLeave",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsHolidayWork",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLate",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOffdayWork",
                table: "AttendanceRecords",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MatchedOfficeLocationId",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Mode",
                table: "AttendanceRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Office");   // valid enum name so existing rows materialise

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "AttendanceRecords",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WorkedMinutes",
                table: "AttendanceRecords",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceBreaks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceRecordId = table.Column<long>(type: "bigint", nullable: false),
                    BreakOutTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    BreakInTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Minutes = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_AttendanceBreaks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceBreaks_AttendanceRecords_AttendanceRecordId",
                        column: x => x.AttendanceRecordId,
                        principalTable: "AttendanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    OfficeStartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    OfficeEndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    GracePeriodMinutes = table.Column<int>(type: "int", nullable: false),
                    OutsideFenceMode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DefaultRadiusMeters = table.Column<int>(type: "int", nullable: false),
                    RequireSelfie = table.Column<bool>(type: "bit", nullable: false),
                    RequireSupervisorApproval = table.Column<bool>(type: "bit", nullable: false),
                    AllowRemote = table.Column<bool>(type: "bit", nullable: false),
                    AllowFieldVisit = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_AttendanceSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSettings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OfficeLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    RadiusMeters = table.Column<double>(type: "float", nullable: false, defaultValue: 10.0),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_OfficeLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OfficeLocations_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOfficeLocations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OfficeLocationId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_EmployeeOfficeLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOfficeLocations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EmployeeOfficeLocations_OfficeLocations_OfficeLocationId",
                        column: x => x.OfficeLocationId,
                        principalTable: "OfficeLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ApprovalStatus",
                table: "AttendanceRecords",
                column: "ApprovalStatus");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_ApprovedByEmployeeId",
                table: "AttendanceRecords",
                column: "ApprovedByEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_MatchedOfficeLocationId",
                table: "AttendanceRecords",
                column: "MatchedOfficeLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceBreaks_AttendanceRecordId",
                table: "AttendanceBreaks",
                column: "AttendanceRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSettings_CompanyId",
                table: "AttendanceSettings",
                column: "CompanyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOfficeLocations_EmployeeId",
                table: "EmployeeOfficeLocations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOfficeLocations_OfficeLocationId",
                table: "EmployeeOfficeLocations",
                column: "OfficeLocationId");

            migrationBuilder.CreateIndex(
                name: "UX_EmployeeOfficeLocations_Pair",
                table: "EmployeeOfficeLocations",
                columns: new[] { "EmployeeId", "OfficeLocationId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OfficeLocations_CompanyId",
                table: "OfficeLocations",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_Employees_ApprovedByEmployeeId",
                table: "AttendanceRecords",
                column: "ApprovedByEmployeeId",
                principalTable: "Employees",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceRecords_OfficeLocations_MatchedOfficeLocationId",
                table: "AttendanceRecords",
                column: "MatchedOfficeLocationId",
                principalTable: "OfficeLocations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_Employees_ApprovedByEmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceRecords_OfficeLocations_MatchedOfficeLocationId",
                table: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "AttendanceBreaks");

            migrationBuilder.DropTable(
                name: "AttendanceSettings");

            migrationBuilder.DropTable(
                name: "EmployeeOfficeLocations");

            migrationBuilder.DropTable(
                name: "OfficeLocations");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ApprovalStatus",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_ApprovedByEmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_MatchedOfficeLocationId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "ApprovedByEmployeeId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInSelfieUrl",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckOutDistanceMeters",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckOutLatitude",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckOutLongitude",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckOutSelfieUrl",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckOutWithinFence",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "FaceMatchScore",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "FaceMatchStatus",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsEarlyLeave",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsHolidayWork",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsLate",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "IsOffdayWork",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "MatchedOfficeLocationId",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "Mode",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "WorkedMinutes",
                table: "AttendanceRecords");
        }
    }
}
