using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFDMasterDataForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM \"FDCashFlows\"");
            migrationBuilder.Sql("DELETE FROM \"FDApprovalHistories\"");
            migrationBuilder.Sql("DELETE FROM \"FDAmendments\"");
            migrationBuilder.Sql("DELETE FROM \"FDInterests\"");
            migrationBuilder.Sql("DELETE FROM \"FDIdentifications\"");
            // ============================================================
            // STEP 1: Add new nullable FK columns on FDIdentifications
            // ============================================================
            migrationBuilder.AddColumn<int>(
                name: "CurrencyId",
                table: "FDIdentifications",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BankId",
                table: "FDIdentifications",
                type: "integer",
                nullable: true);

            // ============================================================
            // STEP 2: Migrate CurrencyCode string → CurrencyId integer
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE ""FDIdentifications"" fd
                SET ""CurrencyId"" = c.""CurrencyId""
                FROM ""Currencies"" c
                WHERE fd.""CurrencyCode"" = c.""CurrencyCode""
            ");

            // Set default for any unmapped rows (fallback to INR or first currency)
            migrationBuilder.Sql(@"
                UPDATE ""FDIdentifications""
                SET ""CurrencyId"" = (SELECT ""CurrencyId"" FROM ""Currencies"" LIMIT 1)
                WHERE ""CurrencyId"" = 0
            ");

            // ============================================================
            // STEP 3: Migrate BankAccountId → BankId (optional FK)
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE ""FDIdentifications""
                SET ""BankId"" = NULL
                WHERE ""BankId"" = 0
            ");

            // ============================================================
            // STEP 4: Alter EntityId and CounterpartyId from bigint → int
            // ============================================================
            migrationBuilder.AlterColumn<int>(
                name: "EntityId",
                table: "FDIdentifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<int>(
                name: "CounterpartyId",
                table: "FDIdentifications",
                type: "integer",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            // ============================================================
            // STEP 5: Drop old string columns from FDIdentifications
            // ============================================================
            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "FDIdentifications");

            migrationBuilder.DropColumn(
                name: "BankAccountId",
                table: "FDIdentifications");

            // ============================================================
            // STEP 6: Add new nullable FK columns on FDInterests
            // ============================================================
            migrationBuilder.AddColumn<int>(
                name: "InterestFrequencyId",
                table: "FDInterests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "CompoundingFrequencyId",
                table: "FDInterests",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DayCountConventionId",
                table: "FDInterests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // ============================================================
            // STEP 7: Migrate InterestFrequency string → InterestFrequencyId
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE ""FDInterests"" fi
                SET ""InterestFrequencyId"" = COALESCE((
                    SELECT intfreq.""Id"" FROM ""InterestFrequencies"" intfreq
                    WHERE UPPER(REPLACE(REPLACE(REPLACE(intfreq.""FrequencyName"", '-', '_'), ' ', '_'), '.', '_'))
                        = UPPER(REPLACE(REPLACE(REPLACE(fi.""InterestFrequency"", '-', '_'), ' ', '_'), '.', '_'))
                    LIMIT 1
                ), 1)
            ");

            // ============================================================
            // STEP 8: Migrate CompoundingFrequency string → CompoundingFrequencyId
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE ""FDInterests"" fi
                SET ""CompoundingFrequencyId"" = (
                    SELECT intfreq.""Id"" FROM ""InterestFrequencies"" intfreq
                    WHERE UPPER(REPLACE(REPLACE(REPLACE(intfreq.""FrequencyName"", '-', '_'), ' ', '_'), '.', '_'))
                        = UPPER(REPLACE(REPLACE(REPLACE(fi.""CompoundingFrequency"", '-', '_'), ' ', '_'), '.', '_'))
                    LIMIT 1
                )
                WHERE fi.""CompoundingFrequency"" IS NOT NULL
                    AND UPPER(fi.""CompoundingFrequency"") NOT IN ('NOT APPLICABLE', 'NOT_APPLICABLE', '')
            ");

            // ============================================================
            // STEP 9: Migrate CalculationBasis string → DayCountConventionId
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE ""FDInterests"" fi
                SET ""DayCountConventionId"" = COALESCE((
                    SELECT dc.""Id"" FROM ""DayCountConventions"" dc
                    WHERE UPPER(REPLACE(dc.""ConventionName"", '/', '_'))
                        = UPPER(REPLACE(fi.""CalculationBasis"", '/', '_'))
                    LIMIT 1
                ), 3)
            ");

            // ============================================================
            // STEP 10: Drop old string columns from FDInterests
            // ============================================================
            migrationBuilder.DropColumn(
                name: "InterestFrequency",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "CompoundingFrequency",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "CalculationBasis",
                table: "FDInterests");

            // ============================================================
            // STEP 11: Alter column constraints for MaxLength properties
            // ============================================================
            migrationBuilder.AlterColumn<string>(
                name: "PaymentConvention",
                table: "FDInterests",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InterestRateType",
                table: "FDInterests",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "BenchmarkName",
                table: "FDInterests",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FDIdentifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "FDIdentifications",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FdReferenceNo",
                table: "FDIdentifications",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            // ============================================================
            // STEP 12: Create indexes
            // ============================================================
            migrationBuilder.CreateIndex(
                name: "IX_FDInterests_CompoundingFrequencyId",
                table: "FDInterests",
                column: "CompoundingFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FDInterests_DayCountConventionId",
                table: "FDInterests",
                column: "DayCountConventionId");

            migrationBuilder.CreateIndex(
                name: "IX_FDInterests_InterestFrequencyId",
                table: "FDInterests",
                column: "InterestFrequencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_BankId",
                table: "FDIdentifications",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_CounterpartyId",
                table: "FDIdentifications",
                column: "CounterpartyId");

            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_CurrencyId",
                table: "FDIdentifications",
                column: "CurrencyId");

            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_EntityId",
                table: "FDIdentifications",
                column: "EntityId");

            // ============================================================
            // STEP 13: Add foreign key constraints
            // ============================================================
            migrationBuilder.AddForeignKey(
                name: "FK_FDIdentifications_Banks_BankId",
                table: "FDIdentifications",
                column: "BankId",
                principalTable: "Banks",
                principalColumn: "BankId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDIdentifications_CounterParties_CounterpartyId",
                table: "FDIdentifications",
                column: "CounterpartyId",
                principalTable: "CounterParties",
                principalColumn: "CounterPartyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDIdentifications_Currencies_CurrencyId",
                table: "FDIdentifications",
                column: "CurrencyId",
                principalTable: "Currencies",
                principalColumn: "CurrencyId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDIdentifications_Entities_EntityId",
                table: "FDIdentifications",
                column: "EntityId",
                principalTable: "Entities",
                principalColumn: "EntityId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDInterests_DayCountConventions_DayCountConventionId",
                table: "FDInterests",
                column: "DayCountConventionId",
                principalTable: "DayCountConventions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDInterests_InterestFrequencies_CompoundingFrequencyId",
                table: "FDInterests",
                column: "CompoundingFrequencyId",
                principalTable: "InterestFrequencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FDInterests_InterestFrequencies_InterestFrequencyId",
                table: "FDInterests",
                column: "InterestFrequencyId",
                principalTable: "InterestFrequencies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FDIdentifications_Banks_BankId",
                table: "FDIdentifications");

            migrationBuilder.DropForeignKey(
                name: "FK_FDIdentifications_CounterParties_CounterpartyId",
                table: "FDIdentifications");

            migrationBuilder.DropForeignKey(
                name: "FK_FDIdentifications_Currencies_CurrencyId",
                table: "FDIdentifications");

            migrationBuilder.DropForeignKey(
                name: "FK_FDIdentifications_Entities_EntityId",
                table: "FDIdentifications");

            migrationBuilder.DropForeignKey(
                name: "FK_FDInterests_DayCountConventions_DayCountConventionId",
                table: "FDInterests");

            migrationBuilder.DropForeignKey(
                name: "FK_FDInterests_InterestFrequencies_CompoundingFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropForeignKey(
                name: "FK_FDInterests_InterestFrequencies_InterestFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropIndex(
                name: "IX_FDInterests_CompoundingFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropIndex(
                name: "IX_FDInterests_DayCountConventionId",
                table: "FDInterests");

            migrationBuilder.DropIndex(
                name: "IX_FDInterests_InterestFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_BankId",
                table: "FDIdentifications");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_CounterpartyId",
                table: "FDIdentifications");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_CurrencyId",
                table: "FDIdentifications");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_EntityId",
                table: "FDIdentifications");

            migrationBuilder.DropColumn(
                name: "CompoundingFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "DayCountConventionId",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "InterestFrequencyId",
                table: "FDInterests");

            migrationBuilder.DropColumn(
                name: "BankId",
                table: "FDIdentifications");

            migrationBuilder.DropColumn(
                name: "CurrencyId",
                table: "FDIdentifications");

            migrationBuilder.AlterColumn<string>(
                name: "PaymentConvention",
                table: "FDInterests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InterestRateType",
                table: "FDInterests",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AlterColumn<string>(
                name: "BenchmarkName",
                table: "FDInterests",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AlterColumn<long>(
                name: "EntityId",
                table: "FDIdentifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<long>(
                name: "CounterpartyId",
                table: "FDIdentifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "FDIdentifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Remarks",
                table: "FDIdentifications",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FdReferenceNo",
                table: "FDIdentifications",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "CalculationBasis",
                table: "FDInterests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CompoundingFrequency",
                table: "FDInterests",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterestFrequency",
                table: "FDInterests",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "BankAccountId",
                table: "FDIdentifications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "FDIdentifications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
