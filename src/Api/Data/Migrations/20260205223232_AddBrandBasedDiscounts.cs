using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBrandBasedDiscounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "products",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "ix_price_rules_tenant_brand_lookup",
                table: "price_rules",
                columns: new[] { "TenantId", "Scope", "TargetId", "BrandId", "Currency", "ValidFrom", "ValidTo", "Priority" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_products_tenant_brand",
                table: "products");

            migrationBuilder.DropIndex(
                name: "ix_price_rules_tenant_brand_lookup",
                table: "price_rules");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "products");

            migrationBuilder.DropColumn(
                name: "BrandId",
                table: "price_rules");
        }
    }
}
