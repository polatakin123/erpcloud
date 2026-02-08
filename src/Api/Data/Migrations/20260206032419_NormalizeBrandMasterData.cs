using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeBrandMasterData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_tenant_brand",
                table: "products");

            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "products",
                type: "uuid",
                nullable: true);

            // Cannot use AlterColumn for string->uuid conversion in PostgreSQL
            // Must use raw SQL with explicit cast
            // First, save old BrandId string values to temp column for migration
            migrationBuilder.Sql(@"
                ALTER TABLE price_rules RENAME COLUMN ""BrandId"" TO ""BrandIdOld"";
            ");

            // Add new uuid BrandId column
            migrationBuilder.AddColumn<Guid>(
                name: "BrandId",
                table: "price_rules",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_brands", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_products_BrandId",
                table: "products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_brandid",
                table: "products",
                columns: new[] { "TenantId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "IX_price_rules_BrandId",
                table: "price_rules",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_brands_TenantId",
                table: "brands",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_brands_TenantId_CreatedAt",
                table: "brands",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_active",
                table: "brands",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_code",
                table: "brands",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_brands_tenant_name",
                table: "brands",
                columns: new[] { "TenantId", "Name" });

            // ==================== DATA MIGRATION ====================
            // Migrate existing Product.Brand string values to Brand entities
            // and update Product.BrandId and PriceRule.BrandId FKs

            migrationBuilder.Sql(@"
                -- Step 1: Create Brand entities from distinct Product.Brand values
                -- Normalize by UPPER(TRIM(Brand)) to avoid duplicates
                INSERT INTO brands (""Id"", ""TenantId"", ""Code"", ""Name"", ""IsActive"", ""CreatedAt"", ""CreatedBy"")
                SELECT 
                    gen_random_uuid() AS ""Id"",
                    p.""TenantId"",
                    UPPER(TRIM(p.""Brand"")) AS ""Code"",
                    TRIM(p.""Brand"") AS ""Name"",
                    true AS ""IsActive"",
                    NOW() AS ""CreatedAt"",
                    '00000000-0000-0000-0000-000000000001'::uuid AS ""CreatedBy""
                FROM products p
                WHERE p.""Brand"" IS NOT NULL 
                  AND TRIM(p.""Brand"") != ''
                GROUP BY p.""TenantId"", UPPER(TRIM(p.""Brand"")), TRIM(p.""Brand"")
                ON CONFLICT (""TenantId"", ""Code"") DO NOTHING;

                -- Step 2: Update Product.BrandId from Brand.Code mapping
                UPDATE products p
                SET ""BrandId"" = b.""Id""
                FROM brands b
                WHERE p.""TenantId"" = b.""TenantId""
                  AND UPPER(TRIM(p.""Brand"")) = b.""Code""
                  AND p.""Brand"" IS NOT NULL
                  AND TRIM(p.""Brand"") != '';

                -- Step 3: Migrate PriceRule.BrandId from old string values
                -- BrandIdOld contains old string values (from renamed column)
                -- Update new BrandId (uuid) column by matching Brand.Code
                UPDATE price_rules pr
                SET ""BrandId"" = b.""Id""
                FROM brands b
                WHERE pr.""TenantId"" = b.""TenantId""
                  AND UPPER(TRIM(pr.""BrandIdOld"")) = b.""Code""
                  AND pr.""BrandIdOld"" IS NOT NULL
                  AND TRIM(pr.""BrandIdOld"") != '';

                -- Step 4: Drop old BrandIdOld column now that data is migrated
                ALTER TABLE price_rules DROP COLUMN IF EXISTS ""BrandIdOld"";
            ");

            // ==================== END DATA MIGRATION ====================

            migrationBuilder.AddForeignKey(
                name: "FK_price_rules_brands_BrandId",
                table: "price_rules",
                column: "BrandId",
                principalTable: "brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_products_brands_BrandId",
                table: "products",
                column: "BrandId",
                principalTable: "brands",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_price_rules_brands_BrandId",
                table: "price_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_products_brands_BrandId",
                table: "products");

            migrationBuilder.DropTable(
                name: "brands");

            migrationBuilder.DropIndex(
                name: "IX_products_BrandId",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_products_tenant_brandid",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_price_rules_BrandId",
                table: "price_rules");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "products");

            // Revert price_rules BrandId back to string type
            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "price_rules");

            migrationBuilder.AddColumn<string>(
                name: "BrandId",
                table: "price_rules",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_products_tenant_brand",
                table: "products",
                columns: new[] { "TenantId", "Brand" });
        }
    }
}
