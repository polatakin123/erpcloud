using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCashBankModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "payments",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "bank_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    BankName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Iban = table.Column<string>(type: "character varying(34)", maxLength: 34, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bank_accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "cash_bank_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SourceType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AmountSigned = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cash_bank_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cash_bank_ledger_entries_payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cashboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cashboxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_payments_tenant_source",
                table: "payments",
                columns: new[] { "TenantId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_TenantId_CreatedAt",
                table: "bank_accounts",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_tenant_active",
                table: "bank_accounts",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_tenant_iban",
                table: "bank_accounts",
                columns: new[] { "TenantId", "Iban" });

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_unique_code",
                table: "bank_accounts",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_bank_accounts_unique_default",
                table: "bank_accounts",
                column: "TenantId",
                unique: true,
                filter: "\"IsDefault\" = true");

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_ledger_entries_PaymentId",
                table: "cash_bank_ledger_entries",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_ledger_entries_TenantId",
                table: "cash_bank_ledger_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_cash_bank_ledger_entries_TenantId_CreatedAt",
                table: "cash_bank_ledger_entries",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_cash_bank_ledger_tenant_payment",
                table: "cash_bank_ledger_entries",
                columns: new[] { "TenantId", "PaymentId" },
                unique: true,
                filter: "\"PaymentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_cash_bank_ledger_tenant_source_date",
                table: "cash_bank_ledger_entries",
                columns: new[] { "TenantId", "SourceType", "SourceId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_cashboxes_TenantId_CreatedAt",
                table: "cashboxes",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_cashboxes_tenant_active",
                table: "cashboxes",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_cashboxes_unique_code",
                table: "cashboxes",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cashboxes_unique_default",
                table: "cashboxes",
                column: "TenantId",
                unique: true,
                filter: "\"IsDefault\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bank_accounts");

            migrationBuilder.DropTable(
                name: "cash_bank_ledger_entries");

            migrationBuilder.DropTable(
                name: "cashboxes");

            migrationBuilder.DropIndex(
                name: "ix_payments_tenant_source",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "payments");
        }
    }
}
