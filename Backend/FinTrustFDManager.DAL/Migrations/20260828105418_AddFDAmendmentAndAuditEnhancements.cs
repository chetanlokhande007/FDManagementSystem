using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FinTrustFDManager.DAL.Migrations
{
    /// <inheritdoc />
    public partial class AddFDAmendmentAndAuditEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FDAmendments",
                columns: table => new
                {
                    AmendmentId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FdId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RequestedValues = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    OriginalValues = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RequestedBy = table.Column<long>(type: "bigint", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<long>(type: "bigint", nullable: true),
                    ApprovedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RejectedBy = table.Column<long>(type: "bigint", nullable: true),
                    RejectedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ApprovalComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RejectionComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FDAmendments", x => x.AmendmentId);
                    table.ForeignKey(
                        name: "FK_FDAmendments_FDIdentifications_FdId",
                        column: x => x.FdId,
                        principalTable: "FDIdentifications",
                        principalColumn: "FdId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FDApprovalHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FdId = table.Column<long>(type: "bigint", nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FromStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ToStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ActionBy = table.Column<long>(type: "bigint", nullable: false),
                    ActionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Comments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OldValues = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    NewValues = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FDApprovalHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FDApprovalHistories_FDIdentifications_FdId",
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
                value: new DateTime(2026, 8, 28, 10, 54, 16, 603, DateTimeKind.Utc).AddTicks(8065));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 28, 10, 54, 16, 603, DateTimeKind.Utc).AddTicks(8068));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 28, 10, 54, 16, 603, DateTimeKind.Utc).AddTicks(8069));

            // Create non-unique index first; unique constraint will be added after data cleanup
            migrationBuilder.CreateIndex(
                name: "IX_FDIdentifications_FdReferenceNo",
                table: "FDIdentifications",
                column: "FdReferenceNo");

            migrationBuilder.CreateIndex(
                name: "IX_FDAmendments_FdId_Status",
                table: "FDAmendments",
                columns: new[] { "FdId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_FDAmendments_RequestedBy",
                table: "FDAmendments",
                column: "RequestedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FDApprovalHistories_FdId_ActionDate",
                table: "FDApprovalHistories",
                columns: new[] { "FdId", "ActionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FDAmendments");

            migrationBuilder.DropTable(
                name: "FDApprovalHistories");

            migrationBuilder.DropIndex(
                name: "IX_FDIdentifications_FdReferenceNo",
                table: "FDIdentifications");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 28, 5, 50, 16, 172, DateTimeKind.Utc).AddTicks(2952));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 28, 5, 50, 16, 172, DateTimeKind.Utc).AddTicks(2953));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedDate",
                value: new DateTime(2026, 8, 28, 5, 50, 16, 172, DateTimeKind.Utc).AddTicks(2955));
        }
    }
}
