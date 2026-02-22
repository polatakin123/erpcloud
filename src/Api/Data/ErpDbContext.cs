using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Data;

/// <summary>
/// Application database context with sample entities.
/// </summary>
public class ErpDbContext : AppDbContext
{
    public ErpDbContext(DbContextOptions<ErpDbContext> options, ITenantContext tenantContext)
        : base(options, tenantContext)
    {
    }

    public DbSet<SampleItem> SampleItems => Set<SampleItem>();
    public DbSet<DemoItem> DemoItems => Set<DemoItem>();
    public DbSet<DemoEventLog> DemoEventLogs => Set<DemoEventLog>();
    
    // Users
    public DbSet<User> Users => Set<User>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();
    
    // Organization module
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    
    // Party module
    public DbSet<Party> Parties => Set<Party>();
    
    // Catalog module
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    
    // Pricing module
    public DbSet<PriceRule> PriceRules => Set<PriceRule>();
    public DbSet<ProductCost> ProductCosts => Set<ProductCost>();
    
    // Stock module
    public DbSet<StockLedgerEntry> StockLedgerEntries => Set<StockLedgerEntry>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    
    // Sales Order module
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => Set<SalesOrderLine>();
    
    // Shipment module
    public DbSet<Shipment> Shipments => Set<Shipment>();
    public DbSet<ShipmentLine> ShipmentLines => Set<ShipmentLine>();
    
    // Accounting module
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => Set<InvoiceLine>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<PartyLedgerEntry> PartyLedgerEntries => Set<PartyLedgerEntry>();
    
    // Returns & Credit Notes module
    public DbSet<SalesReturn> SalesReturns => Set<SalesReturn>();
    public DbSet<SalesReturnLine> SalesReturnLines => Set<SalesReturnLine>();
    public DbSet<PurchaseReturn> PurchaseReturns => Set<PurchaseReturn>();
    public DbSet<PurchaseReturnLine> PurchaseReturnLines => Set<PurchaseReturnLine>();
    public DbSet<CreditNote> CreditNotes => Set<CreditNote>();
    public DbSet<CreditNoteLine> CreditNoteLines => Set<CreditNoteLine>();
    
    // E-Document module
    public DbSet<EDocument> EDocuments => Set<EDocument>();
    public DbSet<EDocumentStatusHistory> EDocumentStatusHistory => Set<EDocumentStatusHistory>();
    
    // Purchase module
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceipt> GoodsReceipts => Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();
    
    // Cash/Bank module
    public DbSet<Cashbox> Cashboxes => Set<Cashbox>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<CashBankLedgerEntry> CashBankLedgerEntries => Set<CashBankLedgerEntry>();
    
    // Part References module (OEM/Equivalent search)
    public DbSet<PartReference> PartReferences => Set<PartReference>();
    
    // Vehicle Fitment module
    public DbSet<VehicleBrand> VehicleBrands => Set<VehicleBrand>();
    public DbSet<VehicleModel> VehicleModels => Set<VehicleModel>();
    public DbSet<VehicleYearRange> VehicleYearRanges => Set<VehicleYearRange>();
    public DbSet<VehicleEngine> VehicleEngines => Set<VehicleEngine>();
    public DbSet<StockCardFitment> StockCardFitments => Set<StockCardFitment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure SampleItem
        modelBuilder.Entity<SampleItem>(entity =>
        {
            entity.ToTable("sample_items");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(e => e.Description)
                .HasMaxLength(1000);

            // Composite index for common queries
            entity.HasIndex(e => new { e.TenantId, e.Name });
        });

        // Configure DemoItem
        modelBuilder.Entity<DemoItem>(entity =>
        {
            entity.ToTable("demo_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");

            entity.HasIndex(e => new { e.TenantId, e.Name });
        });

        // Configure DemoEventLog
        modelBuilder.Entity<DemoEventLog>(entity =>
        {
            entity.ToTable("demo_event_logs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.MessageId).IsRequired();
            entity.Property(e => e.Payload).IsRequired().HasColumnType("jsonb");
            entity.Property(e => e.ProcessedAt).IsRequired();

            // Unique constraint to prevent duplicate processing
            entity.HasIndex(e => new { e.TenantId, e.MessageId })
                .IsUnique()
                .HasDatabaseName("ix_demo_event_logs_tenant_message");
        });
        
        // Configure Permission
        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("permissions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.HasIndex(e => e.Code).IsUnique();
        });
        
        // Configure UserPermission
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.ToTable("user_permissions");
            entity.HasKey(e => new { e.UserId, e.PermissionId });
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.Permission)
                .WithMany(p => p.UserPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Configure Organization
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.ToTable("organizations");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.TaxNumber).HasMaxLength(32);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_organizations_tenant_code");
        });

        // Configure Branch
        modelBuilder.Entity<Branch>(entity =>
        {
            entity.ToTable("branches");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.City).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key
            entity.HasOne(b => b.Organization)
                .WithMany(o => o.Branches)
                .HasForeignKey(b => b.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for queries
            entity.HasIndex(e => new { e.TenantId, e.OrganizationId })
                .HasDatabaseName("ix_branches_tenant_org");

            // Unique constraint: tenant + org + code
            entity.HasIndex(e => new { e.TenantId, e.OrganizationId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_branches_tenant_org_code");
        });

        // Configure Warehouse
        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("warehouses");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(32);
            entity.Property(e => e.IsDefault).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key
            entity.HasOne(w => w.Branch)
                .WithMany(b => b.Warehouses)
                .HasForeignKey(w => w.BranchId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for queries
            entity.HasIndex(e => new { e.TenantId, e.BranchId })
                .HasDatabaseName("ix_warehouses_tenant_branch");

            // Unique constraint: tenant + branch + code
            entity.HasIndex(e => new { e.TenantId, e.BranchId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_warehouses_tenant_branch_code");

            // Partial unique index: only one default per branch
            entity.HasIndex(e => new { e.TenantId, e.BranchId })
                .IsUnique()
                .HasDatabaseName("ix_warehouses_tenant_branch_default")
                .HasFilter("\"IsDefault\" = true");
        });

        // Configure Party
        modelBuilder.Entity<Party>(entity =>
        {
            entity.ToTable("parties");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(16);
            entity.Property(e => e.TaxNumber).HasMaxLength(32);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.CreditLimit).HasColumnType("decimal(18,2)");
            entity.Property(e => e.PaymentTermDays);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_parties_tenant_code");

            // Index for type queries
            entity.HasIndex(e => new { e.TenantId, e.Type })
                .HasDatabaseName("ix_parties_tenant_type");

            // Index for name search
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("ix_parties_tenant_name");
        });

        // Configure Brand (Master Data)
        modelBuilder.Entity<Brand>(entity =>
        {
            entity.ToTable("brands");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Unique constraint: tenant + code (case-insensitive)
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_brands_tenant_code");

            // Index for name search
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("ix_brands_tenant_name");

            // Index for active brands
            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("ix_brands_tenant_active");
        });

        // Configure Product
        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
#pragma warning disable CS0618 // Suppress obsolete warning for migration compatibility
            entity.Property(e => e.Brand).HasMaxLength(100); // DEPRECATED: kept for migration
#pragma warning restore CS0618
            entity.Property(e => e.BrandId); // FK to Brand
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to Brand (optional)
            entity.HasOne(p => p.BrandNavigation)
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_products_tenant_code");

            // Index for name search
            entity.HasIndex(e => new { e.TenantId, e.Name })
                .HasDatabaseName("ix_products_tenant_name");
            
            // Index for brand FK filtering (replaces old string brand index)
            entity.HasIndex(e => new { e.TenantId, e.BrandId })
                .HasDatabaseName("ix_products_tenant_brandid");
        });

        // Configure ProductVariant
        modelBuilder.Entity<ProductVariant>(entity =>
        {
            entity.ToTable("product_variants");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Sku).IsRequired().HasMaxLength(64);
            entity.Property(e => e.Barcode).HasMaxLength(128);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Unit).HasMaxLength(32);
            entity.Property(e => e.VatRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key
            entity.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: tenant + sku
            entity.HasIndex(e => new { e.TenantId, e.Sku })
                .IsUnique()
                .HasDatabaseName("ix_product_variants_tenant_sku");

            // Index for product queries
            entity.HasIndex(e => new { e.TenantId, e.ProductId })
                .HasDatabaseName("ix_product_variants_tenant_product");

            // Index for barcode search
            entity.HasIndex(e => new { e.TenantId, e.Barcode })
                .HasDatabaseName("ix_product_variants_tenant_barcode");
        });

        // Configure PriceList
        modelBuilder.Entity<PriceList>(entity =>
        {
            entity.ToTable("price_lists");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IsDefault).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_price_lists_tenant_code");

            // Partial unique index: only one default per tenant
            entity.HasIndex(e => e.TenantId)
                .IsUnique()
                .HasDatabaseName("ix_price_lists_tenant_default")
                .HasFilter("\"IsDefault\" = true");
        });

        // Configure PriceListItem
        modelBuilder.Entity<PriceListItem>(entity =>
        {
            entity.ToTable("price_list_items");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.MinQty).HasColumnType("decimal(18,3)");
            entity.Property(e => e.ValidFrom);
            entity.Property(e => e.ValidTo);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(i => i.PriceList)
                .WithMany(pl => pl.Items)
                .HasForeignKey(i => i.PriceListId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.Variant)
                .WithMany(v => v.PriceListItems)
                .HasForeignKey(i => i.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Composite unique constraint: prevent duplicate prices
            // Same tenant, price list, variant, minqty, and valid-from date
            entity.HasIndex(e => new { e.TenantId, e.PriceListId, e.VariantId, e.MinQty, e.ValidFrom })
                .IsUnique()
                .HasDatabaseName("ix_price_list_items_unique");

            // Index for price list queries
            entity.HasIndex(e => new { e.TenantId, e.PriceListId })
                .HasDatabaseName("ix_price_list_items_tenant_list");

            // Index for variant queries
            entity.HasIndex(e => new { e.TenantId, e.VariantId })
                .HasDatabaseName("ix_price_list_items_tenant_variant");
        });

        // Configure PriceRule
        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.ToTable("price_rules");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Scope).IsRequired().HasMaxLength(32);
            entity.Property(e => e.RuleType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.TargetId).IsRequired();
            entity.Property(e => e.VariantId);
            entity.Property(e => e.BrandId); // Guid FK to Brand
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Value).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Priority).IsRequired();
            entity.Property(e => e.ValidFrom).IsRequired();
            entity.Property(e => e.ValidTo);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to variant (optional)
            entity.HasOne(r => r.Variant)
                .WithMany()
                .HasForeignKey(r => r.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to brand (optional)
            entity.HasOne(r => r.BrandNavigation)
                .WithMany(b => b.PriceRules)
                .HasForeignKey(r => r.BrandId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for rule queries by scope and target
            entity.HasIndex(e => new { e.TenantId, e.Scope, e.TargetId, e.IsActive })
                .HasDatabaseName("ix_price_rules_tenant_scope_target");

            // Index for variant-specific rules
            entity.HasIndex(e => new { e.TenantId, e.VariantId, e.IsActive })
                .HasDatabaseName("ix_price_rules_tenant_variant");

            // Index for brand-based rules (optimized for brand discount lookup)
            entity.HasIndex(e => new { e.TenantId, e.Scope, e.TargetId, e.BrandId, e.Currency, e.ValidFrom, e.ValidTo, e.Priority })
                .HasDatabaseName("ix_price_rules_tenant_brand_lookup");

            // Index for date range queries
            entity.HasIndex(e => new { e.TenantId, e.ValidFrom, e.ValidTo })
                .HasDatabaseName("ix_price_rules_tenant_validity");
        });

        // Configure ProductCost
        modelBuilder.Entity<ProductCost>(entity =>
        {
            entity.ToTable("product_costs");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VariantId).IsRequired();
            entity.Property(e => e.LastPurchaseCost).IsRequired().HasColumnType("decimal(18,4)");
            entity.Property(e => e.AverageCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.MinSalePrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.LastUpdated).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to variant
            entity.HasOne(c => c.Variant)
                .WithMany()
                .HasForeignKey(c => c.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: one cost record per variant
            entity.HasIndex(e => new { e.TenantId, e.VariantId })
                .IsUnique()
                .HasDatabaseName("ix_product_costs_tenant_variant");
        });

        // Configure StockLedgerEntry
        modelBuilder.Entity<StockLedgerEntry>(entity =>
        {
            entity.ToTable("stock_ledger_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.MovementType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Quantity).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.ReferenceType).HasMaxLength(64);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Primary index for queries (optimized for most common query pattern)
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.VariantId, e.OccurredAt })
                .HasDatabaseName("ix_stock_ledger_tenant_warehouse_variant_occurred");

            // Index for reference lookups
            entity.HasIndex(e => new { e.TenantId, e.ReferenceType, e.ReferenceId })
                .HasDatabaseName("ix_stock_ledger_tenant_reference");

            // Index for correlation lookups (finding related transfer entries)
            entity.HasIndex(e => new { e.TenantId, e.CorrelationId })
                .HasDatabaseName("ix_stock_ledger_tenant_correlation");
        });

        // Configure StockBalance
        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("stock_balances");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OnHand).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.Reserved).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(b => b.Warehouse)
                .WithMany()
                .HasForeignKey(b => b.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(b => b.Variant)
                .WithMany()
                .HasForeignKey(b => b.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: one balance per tenant/warehouse/variant
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId, e.VariantId })
                .IsUnique()
                .HasDatabaseName("ix_stock_balances_unique");

            // Index for warehouse queries
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId })
                .HasDatabaseName("ix_stock_balances_tenant_warehouse");

            // Index for variant queries
            entity.HasIndex(e => new { e.TenantId, e.VariantId })
                .HasDatabaseName("ix_stock_balances_tenant_variant");
        });

        // Configure SalesOrder
        modelBuilder.Entity<SalesOrder>(entity =>
        {
            entity.ToTable("sales_orders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OrderNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(o => o.Party)
                .WithMany()
                .HasForeignKey(o => o.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Branch)
                .WithMany()
                .HasForeignKey(o => o.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.Warehouse)
                .WithMany()
                .HasForeignKey(o => o.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.PriceList)
                .WithMany()
                .HasForeignKey(o => o.PriceListId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + OrderNo
            entity.HasIndex(e => new { e.TenantId, e.OrderNo })
                .IsUnique()
                .HasDatabaseName("ix_sales_orders_tenant_orderno");

            // Index for party queries
            entity.HasIndex(e => new { e.TenantId, e.PartyId })
                .HasDatabaseName("ix_sales_orders_tenant_party");

            // Index for warehouse queries
            entity.HasIndex(e => new { e.TenantId, e.WarehouseId })
                .HasDatabaseName("ix_sales_orders_tenant_warehouse");

            // Index for status queries
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_sales_orders_tenant_status");

            // Index for order date queries (descending for common sorting)
            entity.HasIndex(e => new { e.TenantId, e.OrderDate })
                .IsDescending(false, true)
                .HasDatabaseName("ix_sales_orders_tenant_orderdate");
        });

        // Configure SalesOrderLine
        modelBuilder.Entity<SalesOrderLine>(entity =>
        {
            entity.ToTable("sales_order_lines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Qty).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitPrice).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatRate).IsRequired().HasColumnType("decimal(5,2)");
            entity.Property(e => e.LineTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.ReservedQty).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.Note).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(l => l.SalesOrder)
                .WithMany(o => o.Lines)
                .HasForeignKey(l => l.SalesOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Variant)
                .WithMany()
                .HasForeignKey(l => l.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for order queries
            entity.HasIndex(e => new { e.TenantId, e.SalesOrderId })
                .HasDatabaseName("ix_sales_order_lines_tenant_order");

            // Unique constraint: one line per variant per order
            entity.HasIndex(e => new { e.TenantId, e.SalesOrderId, e.VariantId })
                .IsUnique()
                .HasDatabaseName("ix_sales_order_lines_unique_variant");
        });

        // Configure Invoice
        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.ToTable("invoices");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.InvoiceNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.SourceType).HasMaxLength(32);
            entity.Property(e => e.SourceId);
            entity.Property(e => e.IssueDate).IsRequired();
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Subtotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.GrandTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(i => i.Party)
                .WithMany()
                .HasForeignKey(i => i.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(i => i.Branch)
                .WithMany()
                .HasForeignKey(i => i.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + InvoiceNo
            entity.HasIndex(e => new { e.TenantId, e.InvoiceNo })
                .IsUnique()
                .HasDatabaseName("ix_invoices_tenant_invoiceno");

            // Index for party queries
            entity.HasIndex(e => new { e.TenantId, e.PartyId })
                .HasDatabaseName("ix_invoices_tenant_party");

            // Index for type queries
            entity.HasIndex(e => new { e.TenantId, e.Type })
                .HasDatabaseName("ix_invoices_tenant_type");

            // Index for status queries
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_invoices_tenant_status");

            // Index for invoice date queries (descending for common sorting)
            entity.HasIndex(e => new { e.TenantId, e.IssueDate })
                .IsDescending(false, true)
                .HasDatabaseName("ix_invoices_tenant_issuedate");

            // Index for source-based invoices (e.g., shipment-based)
            entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId })
                .HasDatabaseName("ix_invoices_tenant_source");
        });

        // Configure InvoiceLine
        modelBuilder.Entity<InvoiceLine>(entity =>
        {
            entity.ToTable("invoice_lines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Qty).HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");
            entity.Property(e => e.LineTotal).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.VatRate).IsRequired().HasColumnType("decimal(5,2)");
            entity.Property(e => e.VatAmount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(l => l.Invoice)
                .WithMany(i => i.Lines)
                .HasForeignKey(l => l.InvoiceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.Variant)
                .WithMany()
                .HasForeignKey(l => l.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.ShipmentLine)
                .WithMany()
                .HasForeignKey(l => l.ShipmentLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.SalesOrderLine)
                .WithMany()
                .HasForeignKey(l => l.SalesOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for invoice queries
            entity.HasIndex(e => new { e.TenantId, e.InvoiceId })
                .HasDatabaseName("ix_invoice_lines_tenant_invoice");

            // Index for shipment line queries
            entity.HasIndex(e => new { e.TenantId, e.ShipmentLineId })
                .HasDatabaseName("ix_invoice_lines_tenant_shipmentline");

            // Unique constraint: one invoice line per shipment line
            entity.HasIndex(e => new { e.TenantId, e.ShipmentLineId })
                .IsUnique()
                .HasFilter("[ShipmentLineId] IS NOT NULL")
                .HasDatabaseName("ix_invoice_lines_unique_shipmentline");
        });

        // Configure Payment
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PaymentNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Direction).IsRequired().HasMaxLength(8);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(p => p.Party)
                .WithMany()
                .HasForeignKey(p => p.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Branch)
                .WithMany()
                .HasForeignKey(p => p.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + PaymentNo
            entity.HasIndex(e => new { e.TenantId, e.PaymentNo })
                .IsUnique()
                .HasDatabaseName("ix_payments_tenant_paymentno");

            // Index for party queries
            entity.HasIndex(e => new { e.TenantId, e.PartyId })
                .HasDatabaseName("ix_payments_tenant_party");

            // Index for direction queries
            entity.HasIndex(e => new { e.TenantId, e.Direction })
                .HasDatabaseName("ix_payments_tenant_direction");

            // Index for date queries (descending for common sorting)
            entity.HasIndex(e => new { e.TenantId, e.Date })
                .IsDescending(false, true)
                .HasDatabaseName("ix_payments_tenant_date");
        });

        // Configure PartyLedgerEntry
        modelBuilder.Entity<PartyLedgerEntry>(entity =>
        {
            entity.ToTable("party_ledger_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.SourceId).IsRequired();
            entity.Property(e => e.Description).IsRequired().HasMaxLength(200);
            entity.Property(e => e.AmountSigned).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.OpenAmountSigned).HasColumnType("decimal(18,2)");
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign keys
            entity.HasOne(e => e.Party)
                .WithMany()
                .HasForeignKey(e => e.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + SourceType + SourceId (idempotency)
            entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId })
                .IsUnique()
                .HasDatabaseName("ix_party_ledger_entries_unique_source");

            // Index for party queries (most common: get ledger)
            entity.HasIndex(e => new { e.TenantId, e.PartyId, e.OccurredAt })
                .IsDescending(false, false, true)
                .HasDatabaseName("ix_party_ledger_entries_tenant_party_occurred");
        });

        // Configure EDocument
        modelBuilder.Entity<EDocument>(entity =>
        {
            entity.ToTable("e_documents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.DocumentType).IsRequired().HasMaxLength(16);
            entity.Property(e => e.Scenario).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.ProviderCode).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Uuid).IsRequired();
            entity.Property(e => e.EnvelopeId).HasMaxLength(100);
            entity.Property(e => e.GIBReference).HasMaxLength(100);
            entity.Property(e => e.LastStatusMessage).HasMaxLength(500);
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.LastTriedAt);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to Invoice
            entity.HasOne(e => e.Invoice)
                .WithMany()
                .HasForeignKey(e => e.InvoiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + InvoiceId + DocumentType (one e-document per invoice per type)
            entity.HasIndex(e => new { e.TenantId, e.InvoiceId, e.DocumentType })
                .IsUnique()
                .HasDatabaseName("ix_e_documents_unique_invoice_type");

            // Index for status queries
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_e_documents_tenant_status");

            // Index for provider queries
            entity.HasIndex(e => new { e.TenantId, e.ProviderCode })
                .HasDatabaseName("ix_e_documents_tenant_provider");

            // Index for document type queries
            entity.HasIndex(e => new { e.TenantId, e.DocumentType })
                .HasDatabaseName("ix_e_documents_tenant_type");
        });

        // Configure EDocumentStatusHistory
        modelBuilder.Entity<EDocumentStatusHistory>(entity =>
        {
            entity.ToTable("e_document_status_history");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Status).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Message).HasMaxLength(500);
            entity.Property(e => e.OccurredAt).IsRequired();

            // Foreign key to EDocument
            entity.HasOne(e => e.EDocument)
                .WithMany(d => d.StatusHistory)
                .HasForeignKey(e => e.EDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for queries
            entity.HasIndex(e => new { e.EDocumentId, e.OccurredAt })
                .IsDescending(false, true)
                .HasDatabaseName("ix_e_document_history_doc_occurred");
        });

        // Configure Shipment
        modelBuilder.Entity<Shipment>(entity =>
        {
            entity.ToTable("shipments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ShipmentNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.ShipmentDate).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);

            // Foreign keys
            entity.HasOne(e => e.SalesOrder)
                .WithMany()
                .HasForeignKey(e => e.SalesOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + ShipmentNo
            entity.HasIndex(e => new { e.TenantId, e.ShipmentNo })
                .IsUnique()
                .HasDatabaseName("ix_shipments_unique_shipment_no");

            // Index for sales order queries
            entity.HasIndex(e => new { e.TenantId, e.SalesOrderId })
                .HasDatabaseName("ix_shipments_tenant_order");

            // Index for date queries
            entity.HasIndex(e => new { e.TenantId, e.ShipmentDate })
                .IsDescending(false, true)
                .HasDatabaseName("ix_shipments_tenant_date");

            // Index for status queries
            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_shipments_tenant_status");
        });

        // Configure ShipmentLine
        modelBuilder.Entity<ShipmentLine>(entity =>
        {
            entity.ToTable("shipment_lines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Qty).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.InvoicedQty).IsRequired().HasColumnType("decimal(18,3)").HasDefaultValue(0);
            entity.Property(e => e.Note).HasMaxLength(200);

            // Foreign keys
            entity.HasOne(e => e.Shipment)
                .WithMany(s => s.Lines)
                .HasForeignKey(e => e.ShipmentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.SalesOrderLine)
                .WithMany()
                .HasForeignKey(e => e.SalesOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for shipment queries
            entity.HasIndex(e => new { e.TenantId, e.ShipmentId })
                .HasDatabaseName("ix_shipment_lines_tenant_shipment");

            // Unique constraint: tenant + ShipmentId + SalesOrderLineId
            entity.HasIndex(e => new { e.TenantId, e.ShipmentId, e.SalesOrderLineId })
                .IsUnique()
                .HasDatabaseName("ix_shipment_lines_unique_order_line");
        });

        // Configure PurchaseOrder
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.ToTable("purchase_orders");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PoNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.OrderDate).IsRequired();
            entity.Property(e => e.ExpectedDate);
            entity.Property(e => e.Note).HasMaxLength(500);

            // Foreign keys
            entity.HasOne(e => e.Party)
                .WithMany()
                .HasForeignKey(e => e.PartyId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + PoNo
            entity.HasIndex(e => new { e.TenantId, e.PoNo })
                .IsUnique()
                .HasDatabaseName("ix_purchase_orders_unique_pono");

            // Indexes for queries
            entity.HasIndex(e => new { e.TenantId, e.PartyId })
                .HasDatabaseName("ix_purchase_orders_tenant_party");

            entity.HasIndex(e => new { e.TenantId, e.WarehouseId })
                .HasDatabaseName("ix_purchase_orders_tenant_warehouse");

            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_purchase_orders_tenant_status");

            entity.HasIndex(e => new { e.TenantId, e.OrderDate })
                .IsDescending(false, true)
                .HasDatabaseName("ix_purchase_orders_tenant_date");
        });

        // Configure PurchaseOrderLine
        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.ToTable("purchase_order_lines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Qty).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.VatRate).HasColumnType("decimal(5,2)");
            entity.Property(e => e.ReceivedQty).IsRequired().HasColumnType("decimal(18,3)").HasDefaultValue(0);
            entity.Property(e => e.Note).HasMaxLength(200);

            // Foreign keys
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(p => p.Lines)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for PO queries
            entity.HasIndex(e => new { e.TenantId, e.PurchaseOrderId })
                .HasDatabaseName("ix_purchase_order_lines_tenant_po");

            // Unique constraint: tenant + PurchaseOrderId + VariantId
            entity.HasIndex(e => new { e.TenantId, e.PurchaseOrderId, e.VariantId })
                .IsUnique()
                .HasDatabaseName("ix_purchase_order_lines_unique_variant");
        });

        // Configure GoodsReceipt
        modelBuilder.Entity<GoodsReceipt>(entity =>
        {
            entity.ToTable("goods_receipts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.GrnNo).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(16);
            entity.Property(e => e.ReceiptDate).IsRequired();
            entity.Property(e => e.Note).HasMaxLength(500);

            // Foreign keys
            entity.HasOne(e => e.PurchaseOrder)
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Branch)
                .WithMany()
                .HasForeignKey(e => e.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Warehouse)
                .WithMany()
                .HasForeignKey(e => e.WarehouseId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: tenant + GrnNo
            entity.HasIndex(e => new { e.TenantId, e.GrnNo })
                .IsUnique()
                .HasDatabaseName("ix_goods_receipts_unique_grnno");

            // Indexes for queries
            entity.HasIndex(e => new { e.TenantId, e.PurchaseOrderId })
                .HasDatabaseName("ix_goods_receipts_tenant_po");

            entity.HasIndex(e => new { e.TenantId, e.ReceiptDate })
                .IsDescending(false, true)
                .HasDatabaseName("ix_goods_receipts_tenant_date");

            entity.HasIndex(e => new { e.TenantId, e.Status })
                .HasDatabaseName("ix_goods_receipts_tenant_status");
        });

        // Configure GoodsReceiptLine
        modelBuilder.Entity<GoodsReceiptLine>(entity =>
        {
            entity.ToTable("goods_receipt_lines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Qty).IsRequired().HasColumnType("decimal(18,3)");
            entity.Property(e => e.UnitCost).HasColumnType("decimal(18,4)");
            entity.Property(e => e.Note).HasMaxLength(200);

            // Foreign keys
            entity.HasOne(e => e.GoodsReceipt)
                .WithMany(g => g.Lines)
                .HasForeignKey(e => e.GoodsReceiptId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.PurchaseOrderLine)
                .WithMany()
                .HasForeignKey(e => e.PurchaseOrderLineId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Variant)
                .WithMany()
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for GRN queries
            entity.HasIndex(e => new { e.TenantId, e.GoodsReceiptId })
                .HasDatabaseName("ix_goods_receipt_lines_tenant_grn");

            // Unique constraint: tenant + GoodsReceiptId + PurchaseOrderLineId
            entity.HasIndex(e => new { e.TenantId, e.GoodsReceiptId, e.PurchaseOrderLineId })
                .IsUnique()
                .HasDatabaseName("ix_goods_receipt_lines_unique_po_line");
        });
        
        // Configure Cashbox
        modelBuilder.Entity<Cashbox>(entity =>
        {
            entity.ToTable("cashboxes");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsDefault).IsRequired().HasDefaultValue(false);

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_cashboxes_unique_code");

            // Partial unique constraint: only one default per tenant
            entity.HasIndex(e => e.TenantId)
                .IsUnique()
                .HasDatabaseName("ix_cashboxes_unique_default")
                .HasFilter("\"IsDefault\" = true");

            // Index for active cashboxes
            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("ix_cashboxes_tenant_active");
        });
        
        // Configure BankAccount
        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("bank_accounts");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.BankName).HasMaxLength(200);
            entity.Property(e => e.Iban).HasMaxLength(34);
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);
            entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
            entity.Property(e => e.IsDefault).IsRequired().HasDefaultValue(false);

            // Unique constraint: tenant + code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_bank_accounts_unique_code");

            // Partial unique constraint: only one default per tenant
            entity.HasIndex(e => e.TenantId)
                .IsUnique()
                .HasDatabaseName("ix_bank_accounts_unique_default")
                .HasFilter("\"IsDefault\" = true");

            // Index for active accounts
            entity.HasIndex(e => new { e.TenantId, e.IsActive })
                .HasDatabaseName("ix_bank_accounts_tenant_active");

            // Index for IBAN lookups
            entity.HasIndex(e => new { e.TenantId, e.Iban })
                .HasDatabaseName("ix_bank_accounts_tenant_iban");
        });
        
        // Configure CashBankLedgerEntry
        modelBuilder.Entity<CashBankLedgerEntry>(entity =>
        {
            entity.ToTable("cash_bank_ledger_entries");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.OccurredAt).IsRequired();
            entity.Property(e => e.SourceType).IsRequired().HasMaxLength(16);
            entity.Property(e => e.SourceId).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.AmountSigned).IsRequired().HasColumnType("decimal(18,2)");
            entity.Property(e => e.Currency).IsRequired().HasMaxLength(3);

            // Foreign key to Payment (optional)
            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index for source queries ordered by date
            entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId, e.OccurredAt })
                .HasDatabaseName("ix_cash_bank_ledger_tenant_source_date");

            // Unique constraint: prevent duplicate payment ledger entries
            entity.HasIndex(e => new { e.TenantId, e.PaymentId })
                .IsUnique()
                .HasDatabaseName("ix_cash_bank_ledger_unique_payment")
                .HasFilter("\"PaymentId\" IS NOT NULL");

            // Index for payment lookups
            entity.HasIndex(e => new { e.TenantId, e.PaymentId })
                .HasDatabaseName("ix_cash_bank_ledger_tenant_payment");
        });
        
        // Update Payment configuration (add source indexes)
        modelBuilder.Entity<Payment>(entity =>
        {
            // Add index for source lookups
            entity.HasIndex(e => new { e.TenantId, e.SourceType, e.SourceId })
                .HasDatabaseName("ix_payments_tenant_source");
        });
        
        // Configure PartReference
        modelBuilder.Entity<PartReference>(entity =>
        {
            entity.ToTable("part_references");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VariantId).IsRequired();
            entity.Property(e => e.RefType).IsRequired().HasMaxLength(16);
            entity.Property(e => e.RefCode).IsRequired().HasMaxLength(64);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to ProductVariant
            entity.HasOne(e => e.Variant)
                .WithMany(v => v.PartReferences)
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: prevent duplicate references
            entity.HasIndex(e => new { e.TenantId, e.VariantId, e.RefType, e.RefCode })
                .IsUnique()
                .HasDatabaseName("ix_part_references_unique");

            // Critical index for fast OEM search
            entity.HasIndex(e => new { e.TenantId, e.RefType, e.RefCode })
                .HasDatabaseName("ix_part_references_search");

            // Index for variant lookups
            entity.HasIndex(e => new { e.TenantId, e.VariantId })
                .HasDatabaseName("ix_part_references_variant");
        });

        // ==================== Vehicle Fitment Module ====================

        // Configure VehicleBrand
        modelBuilder.Entity<VehicleBrand>(entity =>
        {
            entity.ToTable("vehicle_brands");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code).IsRequired().HasMaxLength(32);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Unique constraint: TenantId + Code
            entity.HasIndex(e => new { e.TenantId, e.Code })
                .IsUnique()
                .HasDatabaseName("ix_vehicle_brands_tenant_code");

            // Index for brand listing
            entity.HasIndex(e => e.TenantId)
                .HasDatabaseName("ix_vehicle_brands_tenant");
        });

        // Configure VehicleModel
        modelBuilder.Entity<VehicleModel>(entity =>
        {
            entity.ToTable("vehicle_models");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.BrandId).IsRequired();
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to VehicleBrand
            entity.HasOne(e => e.Brand)
                .WithMany(b => b.Models)
                .HasForeignKey(e => e.BrandId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent delete if has models

            // Unique constraint: TenantId + BrandId + Name
            entity.HasIndex(e => new { e.TenantId, e.BrandId, e.Name })
                .IsUnique()
                .HasDatabaseName("ix_vehicle_models_tenant_brand_name");

            // Index for brand lookups
            entity.HasIndex(e => new { e.TenantId, e.BrandId })
                .HasDatabaseName("ix_vehicle_models_tenant_brand");
        });

        // Configure VehicleYearRange
        modelBuilder.Entity<VehicleYearRange>(entity =>
        {
            entity.ToTable("vehicle_year_ranges");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.ModelId).IsRequired();
            entity.Property(e => e.YearFrom).IsRequired();
            entity.Property(e => e.YearTo).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to VehicleModel
            entity.HasOne(e => e.Model)
                .WithMany(m => m.YearRanges)
                .HasForeignKey(e => e.ModelId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent delete if has year ranges

            // Unique constraint: TenantId + ModelId + YearFrom + YearTo
            entity.HasIndex(e => new { e.TenantId, e.ModelId, e.YearFrom, e.YearTo })
                .IsUnique()
                .HasDatabaseName("ix_vehicle_year_ranges_tenant_model_years");

            // Index for model lookups
            entity.HasIndex(e => new { e.TenantId, e.ModelId })
                .HasDatabaseName("ix_vehicle_year_ranges_tenant_model");
        });

        // Configure VehicleEngine
        modelBuilder.Entity<VehicleEngine>(entity =>
        {
            entity.ToTable("vehicle_engines");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.YearRangeId).IsRequired();
            entity.Property(e => e.Code).IsRequired().HasMaxLength(64);
            entity.Property(e => e.FuelType).IsRequired().HasMaxLength(32);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to VehicleYearRange
            entity.HasOne(e => e.YearRange)
                .WithMany(y => y.Engines)
                .HasForeignKey(e => e.YearRangeId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent delete if has engines

            // Unique constraint: TenantId + YearRangeId + Code + FuelType
            entity.HasIndex(e => new { e.TenantId, e.YearRangeId, e.Code, e.FuelType })
                .IsUnique()
                .HasDatabaseName("ix_vehicle_engines_tenant_year_code_fuel");

            // Index for year range lookups
            entity.HasIndex(e => new { e.TenantId, e.YearRangeId })
                .HasDatabaseName("ix_vehicle_engines_tenant_year");
        });

        // Configure StockCardFitment
        modelBuilder.Entity<StockCardFitment>(entity =>
        {
            entity.ToTable("stock_card_fitments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.VariantId).IsRequired();
            entity.Property(e => e.VehicleEngineId).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired();

            // Foreign key to ProductVariant
            entity.HasOne(e => e.Variant)
                .WithMany() // No navigation property on ProductVariant yet
                .HasForeignKey(e => e.VariantId)
                .OnDelete(DeleteBehavior.Cascade);

            // Foreign key to VehicleEngine
            entity.HasOne(e => e.VehicleEngine)
                .WithMany(ve => ve.Fitments)
                .HasForeignKey(e => e.VehicleEngineId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent delete engine if used

            // Unique constraint: TenantId + VariantId + VehicleEngineId
            entity.HasIndex(e => new { e.TenantId, e.VariantId, e.VehicleEngineId })
                .IsUnique()
                .HasDatabaseName("ix_stock_card_fitments_tenant_variant_engine");

            // Index for variant lookups
            entity.HasIndex(e => new { e.TenantId, e.VariantId })
                .HasDatabaseName("ix_stock_card_fitments_tenant_variant");

            // Index for engine lookups
            entity.HasIndex(e => new { e.TenantId, e.VehicleEngineId })
                .HasDatabaseName("ix_stock_card_fitments_tenant_engine");
        });    }
}