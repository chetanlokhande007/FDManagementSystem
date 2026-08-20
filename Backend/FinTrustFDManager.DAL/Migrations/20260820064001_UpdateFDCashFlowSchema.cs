using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFDCashFlowSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GrossInterest",
                table: "FDCashFlows");

            migrationBuilder.DropColumn(
                name: "NetInterest",
                table: "FDCashFlows");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "FDCashFlows",
                newName: "InterestRate");

            migrationBuilder.RenameColumn(
                name: "TdsAmount",
                table: "FDCashFlows",
                newName: "InterestAmount");

            migrationBuilder.RenameColumn(
                name: "PrincipalAmount",
                table: "FDCashFlows",
                newName: "CashFlowAmount");

            migrationBuilder.RenameColumn(
                name: "CashFlowType",
                table: "FDCashFlows",
                newName: "Event");

            migrationBuilder.RenameColumn(
                name: "CashFlowDate",
                table: "FDCashFlows",
                newName: "StartDate");

            migrationBuilder.AddColumn<DateTime>(
                name: "EndDate",
                table: "FDCashFlows",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 20, 6, 40, 0, 226, DateTimeKind.Utc).AddTicks(1372));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 20, 6, 40, 0, 226, DateTimeKind.Utc).AddTicks(1373));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 20, 6, 40, 0, 226, DateTimeKind.Utc).AddTicks(1375));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndDate",
                table: "FDCashFlows");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                table: "FDCashFlows",
                newName: "CashFlowDate");

            migrationBuilder.RenameColumn(
                name: "InterestRate",
                table: "FDCashFlows",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "InterestAmount",
                table: "FDCashFlows",
                newName: "TdsAmount");

            migrationBuilder.RenameColumn(
                name: "Event",
                table: "FDCashFlows",
                newName: "CashFlowType");

            migrationBuilder.RenameColumn(
                name: "CashFlowAmount",
                table: "FDCashFlows",
                newName: "PrincipalAmount");

            migrationBuilder.AddColumn<decimal>(
                name: "GrossInterest",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "NetInterest",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 18, 7, 28, 17, 616, DateTimeKind.Utc).AddTicks(6050));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 18, 7, 28, 17, 616, DateTimeKind.Utc).AddTicks(6052));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 18, 7, 28, 17, 616, DateTimeKind.Utc).AddTicks(6092));
        }
    }
}
