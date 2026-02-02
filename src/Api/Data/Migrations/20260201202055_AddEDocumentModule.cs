using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEDocumentModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "e_documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    DocumentType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProviderCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvelopeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    GIBReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastStatusMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    LastTriedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e_documents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_e_documents_invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "e_document_status_history",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_e_document_status_history", x => x.Id);
                    table.ForeignKey(
                        name: "FK_e_document_status_history_e_documents_EDocumentId",
                        column: x => x.EDocumentId,
                        principalTable: "e_documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_e_document_status_history_TenantId",
                table: "e_document_status_history",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_e_document_status_history_TenantId_CreatedAt",
                table: "e_document_status_history",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_e_document_history_doc_occurred",
                table: "e_document_status_history",
                columns: new[] { "EDocumentId", "OccurredAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_e_documents_InvoiceId",
                table: "e_documents",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_e_documents_TenantId",
                table: "e_documents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_e_documents_TenantId_CreatedAt",
                table: "e_documents",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_e_documents_tenant_provider",
                table: "e_documents",
                columns: new[] { "TenantId", "ProviderCode" });

            migrationBuilder.CreateIndex(
                name: "ix_e_documents_tenant_status",
                table: "e_documents",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "ix_e_documents_tenant_type",
                table: "e_documents",
                columns: new[] { "TenantId", "DocumentType" });

            migrationBuilder.CreateIndex(
                name: "ix_e_documents_unique_invoice_type",
                table: "e_documents",
                columns: new[] { "TenantId", "InvoiceId", "DocumentType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "e_document_status_history");

            migrationBuilder.DropTable(
                name: "e_documents");
        }
    }
}
