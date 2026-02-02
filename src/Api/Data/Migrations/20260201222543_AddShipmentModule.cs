using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ShippedQty",
                table: "sales_order_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentNo = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SalesOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipments_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_sales_orders_SalesOrderId",
                        column: x => x.SalesOrderId,
                        principalTable: "sales_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipments_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "shipment_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesOrderLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric(18,3)", nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipment_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipment_lines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_lines_sales_order_lines_SalesOrderLineId",
                        column: x => x.SalesOrderLineId,
                        principalTable: "sales_order_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shipment_lines_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_lines_SalesOrderLineId",
                table: "shipment_lines",
                column: "SalesOrderLineId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_lines_ShipmentId",
                table: "shipment_lines",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_lines_TenantId",
                table: "shipment_lines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shipment_lines_TenantId_CreatedAt",
                table: "shipment_lines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_shipment_lines_VariantId",
                table: "shipment_lines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_tenant_shipment",
                table: "shipment_lines",
                columns: new[] { "TenantId", "ShipmentId" });

            migrationBuilder.CreateIndex(
                name: "ix_shipment_lines_unique_order_line",
                table: "shipment_lines",
                columns: new[] { "TenantId", "ShipmentId", "SalesOrderLineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_BranchId",
                table: "shipments",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_SalesOrderId",
                table: "shipments",
                column: "SalesOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_TenantId",
                table: "shipments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_shipments_TenantId_CreatedAt",
                table: "shipments",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_shipments_WarehouseId",
                table: "shipments",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_date",
                table: "shipments",
                columns: new[] { "TenantId", "ShipmentDate" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_order",
                table: "shipments",
                columns: new[] { "TenantId", "SalesOrderId" });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_tenant_status",
                table: "shipments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_shipments_unique_shipment_no",
                table: "shipments",
                columns: new[] { "TenantId", "ShipmentNo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shipment_lines");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropColumn(
                name: "ShippedQty",
                table: "sales_order_lines");
        }
    }
}
