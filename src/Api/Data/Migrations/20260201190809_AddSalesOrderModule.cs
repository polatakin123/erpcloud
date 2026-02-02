using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesOrderModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sales_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceListId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_orders_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_price_lists_PriceListId",
                        column: x => x.PriceListId,
                        principalTable: "price_lists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_orders_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sales_order_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    ReservedQty = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sales_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sales_order_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sales_order_lines_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_SalesOrderId",
                table: "sales_order_lines",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_TenantId",
                table: "sales_order_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_TenantId_CreatedAt",
                table: "sales_order_lines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_order_lines_VariantId",
                table: "sales_order_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_lines_tenant_order",
                table: "sales_order_lines",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_order_lines_unique_variant",
                table: "sales_order_lines",
                columns: new[] { "TenantId", "SalesOrderId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_BranchId",
                table: "sales_orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_PartyId",
                table: "sales_orders",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_PriceListId",
                table: "sales_orders",
                column: "PriceListId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_TenantId",
                table: "sales_orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_TenantId_CreatedAt",
                table: "sales_orders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sales_orders_WarehouseId",
                table: "sales_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_orderdate",
                table: "sales_orders",
                columns: new[] { "TenantId", "OrderDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_orderno",
                table: "sales_orders",
                columns: new[] { "TenantId", "OrderNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_party",
                table: "sales_orders",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_status",
                table: "sales_orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_sales_orders_tenant_warehouse",
                table: "sales_orders",
                columns: new[] { "TenantId", "WarehouseId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sales_order_lines");

            migrationBuilder.DropTable(
                name: "sales_orders");
        }
    }
}
