using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    GrandTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoices_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_invoices_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "party_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AmountSigned = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    OpenAmountSigned = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_party_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_party_ledger_entries_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_party_ledger_entries_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Direction = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_payments_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoice_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: true),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_invoice_lines_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoice_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_InvoiceId",
                table: "invoice_lines",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_TenantId",
                table: "invoice_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_TenantId_CreatedAt",
                table: "invoice_lines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_VariantId",
                table: "invoice_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_tenant_invoice",
                table: "invoice_lines",
                columns: new[] { "TenantId", "InvoiceId" });

            migrationBuilder.CreateIndex(
                name: "IX_invoices_BranchId",
                table: "invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_PartyId",
                table: "invoices",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_TenantId",
                table: "invoices",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_TenantId_CreatedAt",
                table: "invoices",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_invoiceno",
                table: "invoices",
                columns: new[] { "TenantId", "InvoiceNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_issuedate",
                table: "invoices",
                columns: new[] { "TenantId", "IssueDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_party",
                table: "invoices",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_status",
                table: "invoices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_type",
                table: "invoices",
                columns: new[] { "TenantId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_BranchId",
                table: "party_ledger_entries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_PartyId",
                table: "party_ledger_entries",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_TenantId",
                table: "party_ledger_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_party_ledger_entries_TenantId_CreatedAt",
                table: "party_ledger_entries",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_party_ledger_entries_tenant_party_occurred",
                table: "party_ledger_entries",
                columns: new[] { "TenantId", "PartyId", "OccurredAt" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_party_ledger_entries_unique_source",
                table: "party_ledger_entries",
                columns: new[] { "TenantId", "SourceType", "SourceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_BranchId",
                table: "payments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_PartyId",
                table: "payments",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId",
                table: "payments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_payments_TenantId_CreatedAt",
                table: "payments",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_date",
                table: "payments",
                columns: new[] { "TenantId", "Date" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_direction",
                table: "payments",
                columns: new[] { "TenantId", "Direction" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_party",
                table: "payments",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_paymentno",
                table: "payments",
                columns: new[] { "TenantId", "PaymentNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_lines");

            migrationBuilder.DropTable(
                name: "party_ledger_entries");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "invoices");
        }
    }
}
