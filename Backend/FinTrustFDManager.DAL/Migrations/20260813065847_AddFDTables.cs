using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFDTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FDIdentifications",
                columns: table => new
                {
                    FdId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FdReferenceNo = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<long>(type: "bigint", nullable: false),
                    CounterpartyId = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyCode = table.Column<string>(type: "text", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    SettlementDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    BankAccountId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Remarks = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ModifiedBy = table.Column<long>(type: "bigint", nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FDIdentifications", x => x.FdId);
                });

            migrationBuilder.CreateTable(
                name: "FDCashFlows",
                columns: table => new
                {
                    CashFlowId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FdId = table.Column<long>(type: "bigint", nullable: false),
                    CashFlowDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    CashFlowType = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    PrincipalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossInterest = table.Column<decimal>(type: "numeric", nullable: false),
                    TdsAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NetInterest = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CurrencyCode = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReferenceNo = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FDCashFlows", x => x.CashFlowId);
                    table.ForeignKey(
                        name: "FK_FDCashFlows_FDIdentifications_FdId",
                        column: x => x.FdId,
                        principalTable: "FDIdentifications",
                        principalColumn: "FdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FDInterests",
                columns: table => new
                {
                    FdInterestId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FdId = table.Column<long>(type: "bigint", nullable: false),
                    InterestRateType = table.Column<string>(type: "text", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    BenchmarkName = table.Column<string>(type: "text", nullable: true),
                    BenchmarkRate = table.Column<decimal>(type: "numeric", nullable: true),
                    Margin = table.Column<decimal>(type: "numeric", nullable: true),
                    InterestFrequency = table.Column<string>(type: "text", nullable: false),
                    CompoundingFrequency = table.Column<string>(type: "text", nullable: true),
                    CalculationBasis = table.Column<string>(type: "text", nullable: false),
                    CalendarCode = table.Column<string>(type: "text", nullable: true),
                    PaymentConvention = table.Column<string>(type: "text", nullable: true),
                    FirstInterestDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    FirstCompoundingDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    TdsApplicable = table.Column<bool>(type: "boolean", nullable: false),
                    TdsRate = table.Column<decimal>(type: "numeric", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FDInterests", x => x.FdInterestId);
                    table.ForeignKey(
                        name: "FK_FDInterests_FDIdentifications_FdId",
                        column: x => x.FdId,
                        principalTable: "FDIdentifications",
                        principalColumn: "FdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 6, 58, 45, 670, DateTimeKind.Utc).AddTicks(1393));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 6, 58, 45, 670, DateTimeKind.Utc).AddTicks(1395));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 13, 6, 58, 45, 670, DateTimeKind.Utc).AddTicks(1396));

            migrationBuilder.CreateIndex(
                name: "IX_FDCashFlows_FdId",
                table: "FDCashFlows",
                column: "FdId");

            migrationBuilder.CreateIndex(
                name: "IX_FDInterests_FdId",
                table: "FDInterests",
                column: "FdId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FDCashFlows");

            migrationBuilder.DropTable(
                name: "FDInterests");

            migrationBuilder.DropTable(
                name: "FDIdentifications");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 12, 17, 18, 22, 577, DateTimeKind.Utc).AddTicks(2383));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 12, 17, 18, 22, 577, DateTimeKind.Utc).AddTicks(2385));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 12, 17, 18, 22, 577, DateTimeKind.Utc).AddTicks(2387));
        }
    }
}
