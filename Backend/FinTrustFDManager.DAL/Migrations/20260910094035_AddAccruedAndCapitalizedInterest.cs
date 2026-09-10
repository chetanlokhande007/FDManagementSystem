using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddAccruedAndCapitalizedInterest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "AccruedInterest",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CapitalizedInterest",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 9, 10, 9, 40, 34, 362, DateTimeKind.Utc).AddTicks(1309));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 9, 10, 9, 40, 34, 362, DateTimeKind.Utc).AddTicks(1311));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 9, 10, 9, 40, 34, 362, DateTimeKind.Utc).AddTicks(1312));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccruedInterest",
                table: "FDCashFlows");

            migrationBuilder.DropColumn(
                name: "CapitalizedInterest",
                table: "FDCashFlows");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 15, 33, 41, 239, DateTimeKind.Utc).AddTicks(3736));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 15, 33, 41, 239, DateTimeKind.Utc).AddTicks(3738));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 15, 33, 41, 239, DateTimeKind.Utc).AddTicks(3740));
        }
    }
}
