using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddCompoundingColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsCompounding",
                table: "FDInterests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "ClosingBalance",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Days",
                table: "FDCashFlows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "OpeningBalance",
                table: "FDCashFlows",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 11, 2, 36, 540, DateTimeKind.Utc).AddTicks(3153));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 11, 2, 36, 540, DateTimeKind.Utc).AddTicks(3154));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 11, 2, 36, 540, DateTimeKind.Utc).AddTicks(3156));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompounding",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "ClosingBalance",
                table: "FDCashFlows");

            migrationBuilder.DropColumn(
                name: "Days",
                table: "FDCashFlows");

            migrationBuilder.DropColumn(
                name: "OpeningBalance",
                table: "FDCashFlows");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 7, 35, 51, 266, DateTimeKind.Utc).AddTicks(8445));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 7, 35, 51, 266, DateTimeKind.Utc).AddTicks(8451));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 7, 35, 51, 266, DateTimeKind.Utc).AddTicks(8453));
        }
    }
}
