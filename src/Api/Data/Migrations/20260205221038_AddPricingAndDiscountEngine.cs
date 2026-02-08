using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPricingAndDiscountEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "price_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuleType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_price_rules_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_costs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastPurchaseCost = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    AverageCost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    MinSalePrice = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_costs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_product_costs_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_price_rules_TenantId",
                table: "price_rules",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_price_rules_TenantId_CreatedAt",
                table: "price_rules",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_price_rules_VariantId",
                table: "price_rules",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_price_rules_tenant_scope_target",
                table: "price_rules",
                columns: new[] { "TenantId", "Scope", "TargetId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_price_rules_tenant_validity",
                table: "price_rules",
                columns: new[] { "TenantId", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "ix_price_rules_tenant_variant",
                table: "price_rules",
                columns: new[] { "TenantId", "VariantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_product_costs_TenantId",
                table: "product_costs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_product_costs_TenantId_CreatedAt",
                table: "product_costs",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_product_costs_VariantId",
                table: "product_costs",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_product_costs_tenant_variant",
                table: "product_costs",
                columns: new[] { "TenantId", "VariantId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "price_rules");

            migrationBuilder.DropTable(
                name: "product_costs");
        }
    }
}
