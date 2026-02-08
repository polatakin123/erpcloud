using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartReferencesForOemSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "part_references",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RefType = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    RefCode = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_part_references", x => x.Id);
                    table.ForeignKey(
                        name: "FK_part_references_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_part_references_TenantId",
                table: "part_references",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_part_references_TenantId_CreatedAt",
                table: "part_references",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_part_references_VariantId",
                table: "part_references",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "ix_part_references_search",
                table: "part_references",
                columns: new[] { "TenantId", "RefType", "RefCode" });

            migrationBuilder.CreateIndex(
                name: "ix_part_references_unique",
                table: "part_references",
                columns: new[] { "TenantId", "VariantId", "RefType", "RefCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_part_references_variant",
                table: "part_references",
                columns: new[] { "TenantId", "VariantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "part_references");
        }
    }
}
