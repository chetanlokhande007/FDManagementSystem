using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class RemoveBankEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FDIdentifications_Banks_BankId",
                table: "FDIdentifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Investments_Banks_BankId",
                table: "Investments");

            migrationBuilder.DropTable(
                name: "Banks");

            migrationBuilder.DropIndex(
                name: "IX_Investments_BankId",
                table: "Investments");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_BankId",
                table: "FDIdentifications");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "Investments");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "FDIdentifications");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "Investments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "FDIdentifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Banks",
                columns: table => new
                {
                    BankId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CountryId = table.Column<int>(type: "integer", nullable: false),
                    BankCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BankName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ModifiedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Banks", x => x.BankId);
                    table.ForeignKey(
                        name: "FK_Banks_Countries_CountryId",
                        column: x => x.CountryId,
                        principalTable: "Countries",
                        principalColumn: "CountryId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 14, 20, 8, 734, DateTimeKind.Utc).AddTicks(806));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 14, 20, 8, 734, DateTimeKind.Utc).AddTicks(809));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 29, 14, 20, 8, 734, DateTimeKind.Utc).AddTicks(811));

            migrationBuilder.CreateIndex(
                name: "IX_Investments_BankId",
                table: "Investments",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_BankId",
                table: "FDIdentifications",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_Banks_BankCode",
                table: "Banks",
                column: "BankCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Banks_CountryId",
                table: "Banks",
                column: "CountryId");

            migrationBuilder.AddForeignKey(
                name: "FK_FDIdentifications_Banks_BankId",
                table: "FDIdentifications",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "BankId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Investments_Banks_BankId",
                table: "Investments",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "BankId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
