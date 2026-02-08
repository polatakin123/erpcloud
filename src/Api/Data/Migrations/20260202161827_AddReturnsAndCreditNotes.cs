using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddReturnsAndCreditNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQty",
                table: "invoice_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReturnedQty",
                table: "goods_receipt_lines",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "CreditNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteNo = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    SourceInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Total = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    AppliedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    RemainingAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNotes_invoices_SourceInvoiceId",
                        column: x => x.SourceInvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNotes_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseReturnNo = table.Column<string>(type: "text", nullable: false),
                    GoodsReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_goods_receipts_GoodsReceiptId",
                        column: x => x.GoodsReceiptId,
                        principalTable: "goods_receipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturns_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReturnNo = table.Column<string>(type: "text", nullable: false),
                    SalesInvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PartyId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturns_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SalesReturns_parties_PartyId",
                        column: x => x.PartyId,
                        principalTable: "parties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturns_warehouses_WarehouseId",
                        column: x => x.WarehouseId,
                        principalTable: "warehouses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CreditNoteLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditNoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Qty = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditNoteLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CreditNoteLines_CreditNotes_CreditNoteId",
                        column: x => x.CreditNoteId,
                        principalTable: "CreditNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CreditNoteLines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PurchaseReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoodsReceiptLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_PurchaseReturns_PurchaseReturnId",
                        column: x => x.PurchaseReturnId,
                        principalTable: "PurchaseReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_goods_receipt_lines_GoodsReceiptLineId",
                        column: x => x.GoodsReceiptLineId,
                        principalTable: "goods_receipt_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PurchaseReturnLines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalesReturnLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SalesReturnId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Qty = table.Column<decimal>(type: "numeric", nullable: false),
                    ReasonCode = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesReturnLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_SalesReturns_SalesReturnId",
                        column: x => x.SalesReturnId,
                        principalTable: "SalesReturns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_invoice_lines_InvoiceLineId",
                        column: x => x.InvoiceLineId,
                        principalTable: "invoice_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SalesReturnLines_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteLines_CreditNoteId",
                table: "CreditNoteLines",
                column: "CreditNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNoteLines_VariantId",
                table: "CreditNoteLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_PartyId",
                table: "CreditNotes",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_SourceInvoiceId",
                table: "CreditNotes",
                column: "SourceInvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId",
                table: "CreditNotes",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditNotes_TenantId_CreatedAt",
                table: "CreditNotes",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_GoodsReceiptLineId",
                table: "PurchaseReturnLines",
                column: "GoodsReceiptLineId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_PurchaseReturnId",
                table: "PurchaseReturnLines",
                column: "PurchaseReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturnLines_VariantId",
                table: "PurchaseReturnLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_GoodsReceiptId",
                table: "PurchaseReturns",
                column: "GoodsReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_PartyId",
                table: "PurchaseReturns",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId",
                table: "PurchaseReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_TenantId_CreatedAt",
                table: "PurchaseReturns",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReturns_WarehouseId",
                table: "PurchaseReturns",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_InvoiceLineId",
                table: "SalesReturnLines",
                column: "InvoiceLineId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_SalesReturnId",
                table: "SalesReturnLines",
                column: "SalesReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturnLines_VariantId",
                table: "SalesReturnLines",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_InvoiceId",
                table: "SalesReturns",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_PartyId",
                table: "SalesReturns",
                column: "PartyId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_TenantId",
                table: "SalesReturns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_TenantId_CreatedAt",
                table: "SalesReturns",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SalesReturns_WarehouseId",
                table: "SalesReturns",
                column: "WarehouseId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CreditNoteLines");

            migrationBuilder.DropTable(
                name: "PurchaseReturnLines");

            migrationBuilder.DropTable(
                name: "SalesReturnLines");

            migrationBuilder.DropTable(
                name: "CreditNotes");

            migrationBuilder.DropTable(
                name: "PurchaseReturns");

            migrationBuilder.DropTable(
                name: "SalesReturns");

            migrationBuilder.DropColumn(
                name: "ReturnedQty",
                table: "invoice_lines");

            migrationBuilder.DropColumn(
                name: "ReturnedQty",
                table: "goods_receipt_lines");
        }
    }
}
