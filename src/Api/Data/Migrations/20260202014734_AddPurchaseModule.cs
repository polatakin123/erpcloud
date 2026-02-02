using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "InvoicedQty",
                table: "shipment_lines",
                type: "numeric(18,3)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<Guid>(
                name: "SourceId",
                table: "invoices",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "invoices",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SalesOrderLineId",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentLineId",
                table: "invoice_lines",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "purchase_orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PoNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    OrderDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ExpectedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_orders_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_orders_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GrnNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipts_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipts_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "purchase_order_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    VatRate = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    ReceivedQty = table.Column<decimal>(type: "numeric(18,3)", nullable: false, defaultValue: 0m),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_purchase_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_purchase_order_lines_purchase_orders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "purchase_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "goods_receipt_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "numeric(18,4)", nullable: true),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_goods_receipt_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_goods_receipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_goods_receipt_lines_purchase_order_lines_PurchaseOrderLineId",
                        column: x => x.PurchaseOrderLineId,
                        principalTable: "purchase_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invoices_tenant_source",
                table: "invoices",
                columns: new[] { "TenantId", "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_SalesOrderLineId",
                table: "invoice_lines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_invoice_lines_ShipmentLineId",
                table: "invoice_lines",
                column: "ShipmentLineId");

            migrationBuilder.CreateIndex(
                name: "ix_invoice_lines_unique_shipmentline",
                table: "invoice_lines",
                columns: new[] { "TenantId", "ShipmentLineId" },
                unique: true,
                filter: "\"ShipmentLineId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_GoodsReceiptId",
                table: "goods_receipt_lines",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_PurchaseOrderLineId",
                table: "goods_receipt_lines",
                column: "PurchaseOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_TenantId",
                table: "goods_receipt_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_TenantId_CreatedAt",
                table: "goods_receipt_lines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_VariantId",
                table: "goods_receipt_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_tenant_grn",
                table: "goods_receipt_lines",
                columns: new[] { "TenantId", "GoodsReceiptId" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipt_lines_unique_po_line",
                table: "goods_receipt_lines",
                columns: new[] { "TenantId", "GoodsReceiptId", "PurchaseOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_BranchId",
                table: "goods_receipts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_PurchaseOrderId",
                table: "goods_receipts",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_TenantId",
                table: "goods_receipts",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_TenantId_CreatedAt",
                table: "goods_receipts",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipts_WarehouseId",
                table: "goods_receipts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_tenant_date",
                table: "goods_receipts",
                columns: new[] { "TenantId", "ReceiptDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_tenant_po",
                table: "goods_receipts",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_tenant_status",
                table: "goods_receipts",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_goods_receipts_unique_grnno",
                table: "goods_receipts",
                columns: new[] { "TenantId", "GrnNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId",
                table: "purchase_order_lines",
                column: "PurchaseOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_TenantId",
                table: "purchase_order_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_TenantId_CreatedAt",
                table: "purchase_order_lines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_VariantId",
                table: "purchase_order_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_tenant_po",
                table: "purchase_order_lines",
                columns: new[] { "TenantId", "PurchaseOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_order_lines_unique_variant",
                table: "purchase_order_lines",
                columns: new[] { "TenantId", "PurchaseOrderId", "VariantId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_BranchId",
                table: "purchase_orders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_PartyId",
                table: "purchase_orders",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId",
                table: "purchase_orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_TenantId_CreatedAt",
                table: "purchase_orders",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_WarehouseId",
                table: "purchase_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_date",
                table: "purchase_orders",
                columns: new[] { "TenantId", "OrderDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_party",
                table: "purchase_orders",
                columns: new[] { "TenantId", "PartyId" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_status",
                table: "purchase_orders",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_tenant_warehouse",
                table: "purchase_orders",
                columns: new[] { "TenantId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "ix_purchase_orders_unique_pono",
                table: "purchase_orders",
                columns: new[] { "TenantId", "PoNo" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_lines_sales_order_lines_SalesOrderLineId",
                table: "invoice_lines",
                column: "SalesOrderLineId",
                principalTable: "sales_order_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_invoice_lines_shipment_lines_ShipmentLineId",
                table: "invoice_lines",
                column: "ShipmentLineId",
                principalTable: "shipment_lines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoice_lines_sales_order_lines_SalesOrderLineId",
                table: "invoice_lines");

            migrationBuilder.DropForeignKey(
                name: "FK_invoice_lines_shipment_lines_ShipmentLineId",
                table: "invoice_lines");

            migrationBuilder.DropTable(
                name: "goods_receipt_lines");

            migrationBuilder.DropTable(
                name: "goods_receipts");

            migrationBuilder.DropTable(
                name: "purchase_order_lines");

            migrationBuilder.DropTable(
                name: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "ix_invoices_tenant_source",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_SalesOrderLineId",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "IX_invoice_lines_ShipmentLineId",
                table: "invoice_lines");

            migrationBuilder.DropIndex(
                name: "ix_invoice_lines_unique_shipmentline",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "InvoicedQty",
                table: "shipment_lines");

            migrationBuilder.DropColumn(
                name: "SourceId",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "SalesOrderLineId",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "ShipmentLineId",
                table: "invoice_lines");
        }
    }
}
