using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceLocationIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CheckInAddress",
                table: "AttendanceRecords",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInBrowser",
                table: "AttendanceRecords",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInDeviceType",
                table: "AttendanceRecords",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInIpAddress",
                table: "AttendanceRecords",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CheckInIsProxyVpn",
                table: "AttendanceRecords",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInIsp",
                table: "AttendanceRecords",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInNetworkNote",
                table: "AttendanceRecords",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CheckInOs",
                table: "AttendanceRecords",
                type: "nvarchar(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CheckInIsProxyVpn",
                table: "AttendanceRecords",
                column: "CheckInIsProxyVpn");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AttendanceRecords_CheckInIsProxyVpn",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInAddress",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInBrowser",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInDeviceType",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInIpAddress",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInIsProxyVpn",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInIsp",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInNetworkNote",
                table: "AttendanceRecords");

            migrationBuilder.DropColumn(
                name: "CheckInOs",
                table: "AttendanceRecords");
        }
    }
}
