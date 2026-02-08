using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpCloud.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddVehicleFitmentSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "vehicle_brands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_brands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_models",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BrandId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_models", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_models_vehicle_brands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "vehicle_brands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_year_ranges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModelId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearFrom = table.Column<int>(type: "integer", nullable: false),
                    YearTo = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_year_ranges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_year_ranges_vehicle_models_ModelId",
                        column: x => x.ModelId,
                        principalTable: "vehicle_models",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_engines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    YearRangeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    FuelType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_engines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_vehicle_engines_vehicle_year_ranges_YearRangeId",
                        column: x => x.YearRangeId,
                        principalTable: "vehicle_year_ranges",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_card_fitments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: false),
                    VehicleEngineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_card_fitments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_stock_card_fitments_product_variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "product_variants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_stock_card_fitments_vehicle_engines_VehicleEngineId",
                        column: x => x.VehicleEngineId,
                        principalTable: "vehicle_engines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_stock_card_fitments_TenantId",
                table: "stock_card_fitments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_card_fitments_TenantId_CreatedAt",
                table: "stock_card_fitments",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_card_fitments_VariantId",
                table: "stock_card_fitments",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_card_fitments_VehicleEngineId",
                table: "stock_card_fitments",
                column: "VehicleEngineId");

            migrationBuilder.CreateIndex(
                name: "ix_stock_card_fitments_tenant_engine",
                table: "stock_card_fitments",
                columns: new[] { "TenantId", "VehicleEngineId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_card_fitments_tenant_variant",
                table: "stock_card_fitments",
                columns: new[] { "TenantId", "VariantId" });

            migrationBuilder.CreateIndex(
                name: "ix_stock_card_fitments_tenant_variant_engine",
                table: "stock_card_fitments",
                columns: new[] { "TenantId", "VariantId", "VehicleEngineId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_brands_TenantId_CreatedAt",
                table: "vehicle_brands",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_brands_tenant",
                table: "vehicle_brands",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_brands_tenant_code",
                table: "vehicle_brands",
                columns: new[] { "TenantId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_engines_TenantId",
                table: "vehicle_engines",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_engines_TenantId_CreatedAt",
                table: "vehicle_engines",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_engines_YearRangeId",
                table: "vehicle_engines",
                column: "YearRangeId");

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_engines_tenant_year",
                table: "vehicle_engines",
                columns: new[] { "TenantId", "YearRangeId" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_engines_tenant_year_code_fuel",
                table: "vehicle_engines",
                columns: new[] { "TenantId", "YearRangeId", "Code", "FuelType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_BrandId",
                table: "vehicle_models",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_TenantId",
                table: "vehicle_models",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_models_TenantId_CreatedAt",
                table: "vehicle_models",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_tenant_brand",
                table: "vehicle_models",
                columns: new[] { "TenantId", "BrandId" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_models_tenant_brand_name",
                table: "vehicle_models",
                columns: new[] { "TenantId", "BrandId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_year_ranges_ModelId",
                table: "vehicle_year_ranges",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_year_ranges_TenantId",
                table: "vehicle_year_ranges",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_year_ranges_TenantId_CreatedAt",
                table: "vehicle_year_ranges",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_year_ranges_tenant_model",
                table: "vehicle_year_ranges",
                columns: new[] { "TenantId", "ModelId" });

            migrationBuilder.CreateIndex(
                name: "ix_vehicle_year_ranges_tenant_model_years",
                table: "vehicle_year_ranges",
                columns: new[] { "TenantId", "ModelId", "YearFrom", "YearTo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stock_card_fitments");

            migrationBuilder.DropTable(
                name: "vehicle_engines");

            migrationBuilder.DropTable(
                name: "vehicle_year_ranges");

            migrationBuilder.DropTable(
                name: "vehicle_models");

            migrationBuilder.DropTable(
                name: "vehicle_brands");
        }
    }
}
