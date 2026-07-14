using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FilterBusinessKeyUniqueIndexesOnSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payslips_EmployeeId_Year_Month",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year",
                table: "LeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_Date_Name",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_FestivalBonuses_EmployeeId_BonusYear_BonusType",
                table: "FestivalBonuses");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_EmployeeId_AttendanceDate",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId_Year_Month",
                table: "Payslips",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "Year" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date_Name",
                table: "Holidays",
                columns: new[] { "Date", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_FestivalBonuses_EmployeeId_BonusYear_BonusType",
                table: "FestivalBonuses",
                columns: new[] { "EmployeeId", "BonusYear", "BonusType" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_AttendanceDate",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payslips_EmployeeId_Year_Month",
                table: "Payslips");

            migrationBuilder.DropIndex(
                name: "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year",
                table: "LeaveBalances");

            migrationBuilder.DropIndex(
                name: "IX_Holidays_Date_Name",
                table: "Holidays");

            migrationBuilder.DropIndex(
                name: "IX_FestivalBonuses_EmployeeId_BonusYear_BonusType",
                table: "FestivalBonuses");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_EmployeeId_AttendanceDate",
                table: "AttendanceRecords");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_EmployeeId_Year_Month",
                table: "Payslips",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_EmployeeId_LeaveTypeId_Year",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "Year" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Holidays_Date_Name",
                table: "Holidays",
                columns: new[] { "Date", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FestivalBonuses_EmployeeId_BonusYear_BonusType",
                table: "FestivalBonuses",
                columns: new[] { "EmployeeId", "BonusYear", "BonusType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_EmployeeId_AttendanceDate",
                table: "AttendanceRecords",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true);
        }
    }
}
