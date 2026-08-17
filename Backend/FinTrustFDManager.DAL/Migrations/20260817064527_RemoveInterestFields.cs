using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInterestFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalendarCode",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "FirstCompoundingDate",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "FirstInterestDate",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "TdsApplicable",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "TdsRate",
                table: "FDInterests");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 6, 45, 25, 245, DateTimeKind.Utc).AddTicks(9496));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 6, 45, 25, 245, DateTimeKind.Utc).AddTicks(9497));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 17, 6, 45, 25, 245, DateTimeKind.Utc).AddTicks(9499));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalendarCode",
                table: "FDInterests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstCompoundingDate",
                table: "FDInterests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstInterestDate",
                table: "FDInterests",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TdsApplicable",
                table: "FDInterests",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TdsRate",
                table: "FDInterests",
                type: "numeric",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 9, 26, 7, 685, DateTimeKind.Utc).AddTicks(8435));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 9, 26, 7, 685, DateTimeKind.Utc).AddTicks(8437));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 9, 26, 7, 685, DateTimeKind.Utc).AddTicks(8439));
        }
    }
}
