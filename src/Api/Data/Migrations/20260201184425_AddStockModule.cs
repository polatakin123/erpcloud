using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "stock_balances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OnHand = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Reserved = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_balances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_balances_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_balances_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    ReferenceType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    CorrelationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_ledger_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_ledger_entries_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_ledger_entries_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_TenantId",
                table: "stock_balances",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_TenantId_CreatedAt",
                table: "stock_balances",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_VariantId",
                table: "stock_balances",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_balances_WarehouseId",
                table: "stock_balances",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_tenant_variant",
                table: "stock_balances",
                columns: new[] { "TenantId", "VariantId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_tenant_warehouse",
                table: "stock_balances",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_balances_unique",
                table: "stock_balances",
                columns: new[] { "TenantId", "WarehouseId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_entries_TenantId",
                table: "stock_ledger_entries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_entries_TenantId_CreatedAt",
                table: "stock_ledger_entries",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_entries_VariantId",
                table: "stock_ledger_entries",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_ledger_entries_WarehouseId",
                table: "stock_ledger_entries",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_tenant_correlation",
                table: "stock_ledger_entries",
                columns: new[] { "TenantId", "CorrelationId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_tenant_reference",
                table: "stock_ledger_entries",
                columns: new[] { "TenantId", "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_ledger_tenant_warehouse_variant_occurred",
                table: "stock_ledger_entries",
                columns: new[] { "TenantId", "WarehouseId", "VariantId", "OccurredAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_balances");

            migrationBuilder.DropTable(
                name: "stock_ledger_entries");
        }
    }
}
