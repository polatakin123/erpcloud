using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ErpCloud.Api.Tests;

public class ReportsModuleTests
{
    private readonly TestTenantContext _tenantContextA;
    private readonly TestDbFactory _dbFactory;

    public ReportsModuleTests()
    {
        _tenantContextA = new TestTenantContext 
        { 
            TenantId = Guid.NewGuid(), 
            UserId = Guid.NewGuid(),
            IsBypassEnabled = true 
        };
        _dbFactory = new TestDbFactory(_tenantContextA);
    }

    [Fact]
    public async Task StockBalances_WithEmptyWarehouse_ReturnsEmpty()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        var service = new ReportsService(context);

        var result = await service.GetStockBalancesAsync(Guid.NewGuid(), null, 1, 50);
        
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task StockMovements_DateRangeInclusive_Works()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        
        var org = new Organization { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "ORG", Name = "Org", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var branch = new Branch { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, OrganizationId = org.Id, Code = "BR", Name = "Branch", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var warehouse = new Warehouse { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, BranchId = branch.Id, Code = "WH", Name = "Warehouse", Type = "MAIN", IsDefault = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var product = new Product { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "P1", Name = "Product", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var variant = new ProductVariant { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, ProductId = product.Id, Sku = "SKU1", Name = "Variant", Unit = "PCS", VatRate = 18m, IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        
        context.Organizations.Add(org);
        context.Branches.Add(branch);
        context.Warehouses.Add(warehouse);
        context.Products.Add(product);
        context.ProductVariants.Add(variant);

        var targetDate = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        var movement = new StockLedgerEntry
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContextA.TenantId,
            WarehouseId = warehouse.Id,
            VariantId = variant.Id,
            OccurredAt = targetDate,
            MovementType = "IN",
            Quantity = 20,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.StockLedgerEntries.Add(movement);
        await context.SaveChangesAsync();

        var service = new ReportsService(context);
        var result = await service.GetStockMovementsAsync(warehouse.Id, null, null, new DateTime(2026, 2, 1), new DateTime(2026, 2, 1), 1, 50);

        Assert.Single(result.Items);
        Assert.Equal(20, result.Items[0].Quantity);
    }

    [Fact]
    public async Task SalesSummary_GroupByDay_Works()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        
        var org = new Organization { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "ORG", Name = "Org", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var branch = new Branch { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, OrganizationId = org.Id, Code = "BR", Name = "Branch", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var party = new Party { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "CUST", Name = "Customer", Type = "CUSTOMER", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        
        context.Organizations.Add(org);
        context.Branches.Add(branch);
        context.Parties.Add(party);

        var targetDate = new DateTime(2026, 2, 1);
        for (int i = 0; i < 2; i++)
        {
            var invoice = new Invoice
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContextA.TenantId,
                Type = "SALES",
                Status = "ISSUED",
                InvoiceNo = $"INV-{i + 1}",
                IssueDate = targetDate,
                PartyId = party.Id,
                BranchId = branch.Id,
                Currency = "TRY",
                Subtotal = 100m,
                VatTotal = 18m,
                GrandTotal = 118m,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            };
            context.Invoices.Add(invoice);
        }
        await context.SaveChangesAsync();

        var service = new ReportsService(context);
        var result = await service.GetSalesSummaryAsync(targetDate, targetDate, "DAY");

        Assert.Single(result);
        Assert.Equal(2, result[0].InvoiceCount);
        Assert.Equal(200m, result[0].TotalNet);
    }

    [Fact]
    public async Task PartyBalances_AggregatesCorrectly()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        
        var org = new Organization { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "ORG", Name = "Org", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var branch = new Branch { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, OrganizationId = org.Id, Code = "BR", Name = "Branch", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var party = new Party { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "CUST", Name = "Customer", Type = "CUSTOMER", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        
        context.Organizations.Add(org);
        context.Branches.Add(branch);
        context.Parties.Add(party);

        var ledgerEntries = new[]
        {
            new PartyLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContextA.TenantId,
                PartyId = party.Id,
                BranchId = branch.Id,
                OccurredAt = DateTime.UtcNow,
                AmountSigned = 1000m,
                Currency = "TRY",
                SourceType = "INVOICE",
                SourceId = Guid.NewGuid(),
                Description = "Invoice",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            },
            new PartyLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContextA.TenantId,
                PartyId = party.Id,
                BranchId = branch.Id,
                OccurredAt = DateTime.UtcNow,
                AmountSigned = -300m,
                Currency = "TRY",
                SourceType = "PAYMENT",
                SourceId = Guid.NewGuid(),
                Description = "Payment",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            }
        };
        context.PartyLedgerEntries.AddRange(ledgerEntries);
        await context.SaveChangesAsync();

        var service = new ReportsService(context);
        var result = await service.GetPartyBalancesAsync(null, null, 1, 50, null);

        Assert.Single(result.Items);
        Assert.Equal(700m, result.Items[0].Balance);
    }

    [Fact]
    public async Task PartyAging_CalculatesBuckets()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        
        var org = new Organization { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "ORG", Name = "Org", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var branch = new Branch { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, OrganizationId = org.Id, Code = "BR", Name = "Branch", CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        var party = new Party { Id = Guid.NewGuid(), TenantId = _tenantContextA.TenantId, Code = "CUST", Name = "Customer", Type = "CUSTOMER", IsActive = true, CreatedAt = DateTime.UtcNow, CreatedBy = Guid.NewGuid() };
        
        context.Organizations.Add(org);
        context.Branches.Add(branch);
        context.Parties.Add(party);

        var asOfDate = new DateTime(2026, 2, 1);
        var invoice1 = new Invoice
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContextA.TenantId,
            Type = "SALES",
            Status = "ISSUED",
            InvoiceNo = "INV-001",
            IssueDate = asOfDate.AddDays(-45),
            DueDate = asOfDate.AddDays(-15),
            PartyId = party.Id,
            BranchId = branch.Id,
            Currency = "TRY",
            Subtotal = 100m,
            VatTotal = 18m,
            GrandTotal = 118m,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Invoices.Add(invoice1);
        await context.SaveChangesAsync();

        var service = new ReportsService(context);
        var result = await service.GetPartyAgingAsync(null, null, 1, 50, asOfDate);

        Assert.Single(result.Items);
        Assert.Equal(118m, result.Items[0].Bucket0_30);
    }

    [Fact]
    public async Task CashBankBalances_AggregatesCorrectly()
    {
        using var context = _dbFactory.CreateContext(_tenantContextA);
        
        var cashbox = new Cashbox
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContextA.TenantId,
            Code = "CASH001",
            Name = "Main Cashbox",
            Currency = "TRY",
            IsActive = true,
            IsDefault = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = Guid.NewGuid()
        };
        context.Cashboxes.Add(cashbox);

        var entries = new[]
        {
            new CashBankLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContextA.TenantId,
                SourceType = "CASHBOX",
                SourceId = cashbox.Id,
                OccurredAt = DateTime.UtcNow,
                AmountSigned = 1000m,
                Currency = "TRY",
                Description = "Payment",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            },
            new CashBankLedgerEntry
            {
                Id = Guid.NewGuid(),
                TenantId = _tenantContextA.TenantId,
                SourceType = "CASHBOX",
                SourceId = cashbox.Id,
                OccurredAt = DateTime.UtcNow,
                AmountSigned = -250m,
                Currency = "TRY",
                Description = "Payment Out",
                CreatedAt = DateTime.UtcNow,
                CreatedBy = Guid.NewGuid()
            }
        };
        context.CashBankLedgerEntries.AddRange(entries);
        await context.SaveChangesAsync();

        var service = new ReportsService(context);
        var result = await service.GetCashBankBalancesAsync(null);

        Assert.Single(result);
        Assert.Equal(750m, result[0].Balance);
    }
}
