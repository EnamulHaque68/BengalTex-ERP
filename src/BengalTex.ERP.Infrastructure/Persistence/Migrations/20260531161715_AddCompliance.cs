using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BengalTex.ERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCompliance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ComplianceAudits",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuditType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Auditor = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ScheduledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ActualDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Result = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Score = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
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
                    table.PrimaryKey("PK_ComplianceAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ComplianceCertificates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CertificateType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    IssuingAuthority = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IssuedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("PK_ComplianceCertificates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditFindings",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComplianceAuditId = table.Column<long>(type: "bigint", nullable: false),
                    FindingDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CorrectiveAction = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedToEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ClosureDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("PK_AuditFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditFindings_ComplianceAudits_ComplianceAuditId",
                        column: x => x.ComplianceAuditId,
                        principalTable: "ComplianceAudits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditFindings_Employees_AssignedToEmployeeId",
                        column: x => x.AssignedToEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_AssignedToEmployeeId",
                table: "AuditFindings",
                column: "AssignedToEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_ComplianceAuditId",
                table: "AuditFindings",
                column: "ComplianceAuditId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_DueDate",
                table: "AuditFindings",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_Severity",
                table: "AuditFindings",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_AuditFindings_Status",
                table: "AuditFindings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_AuditType",
                table: "ComplianceAudits",
                column: "AuditType");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_Code",
                table: "ComplianceAudits",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_ScheduledDate",
                table: "ComplianceAudits",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceAudits_Status",
                table: "ComplianceAudits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceCertificates_CertificateType",
                table: "ComplianceCertificates",
                column: "CertificateType");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceCertificates_ExpiryDate",
                table: "ComplianceCertificates",
                column: "ExpiryDate");

            migrationBuilder.CreateIndex(
                name: "IX_ComplianceCertificates_IsActive",
                table: "ComplianceCertificates",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditFindings");

            migrationBuilder.DropTable(
                name: "ComplianceCertificates");

            migrationBuilder.DropTable(
                name: "ComplianceAudits");
        }
    }
}
