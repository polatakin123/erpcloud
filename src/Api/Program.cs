using ErpCloud.Api.Data;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Auth;
using ErpCloud.BuildingBlocks.Messaging;
using ErpCloud.BuildingBlocks.Outbox;
using ErpCloud.BuildingBlocks.Persistence;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;
using Serilog;
using FluentValidation;
using FluentValidation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Add Database Context with AuditInterceptor
builder.Services.AddScoped<AuditInterceptor>();
builder.Services.AddDbContext<ErpDbContext>((serviceProvider, options) =>
{
    var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(auditInterceptor);
});

// Add Tenant Context
builder.Services.AddTenantContext();

// Add Outbox Pattern
builder.Services.AddOutbox<ErpDbContext>();

// Add RabbitMQ Messaging - TEMPORARILY DISABLED (RabbitMQ not running)
// builder.Services.AddRabbitMq(builder.Configuration);

// Add Background Services - TEMPORARILY DISABLED FOR DESKTOP APP TESTING
// builder.Services.AddHostedService<OutboxDispatcherService>();
// builder.Services.AddSingleton<DemoEventConsumer>();
// builder.Services.AddHostedService<DemoEventConsumerHostedService>();

// Add Authentication & Authorization
builder.Services.AddErpAuth(builder.Configuration);

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPermissionPolicies(
        ("stock.read", "stock.read"),
        ("stock.write", "stock.write"),
        ("salesorder.read", "salesorder.read"),
        ("salesorder.write", "salesorder.write"),
        ("shipment.read", "shipment.read"),
        ("shipment.write", "shipment.write"),
        ("invoicing.write", "invoicing.write"),
        ("order.read", "order.read"),
        ("order.write", "order.write"),
        ("org.read", "org.read"),
        ("org.write", "org.write"),
        ("branch.read", "branch.read"),
        ("branch.write", "branch.write"),
        ("warehouse.read", "warehouse.read"),
        ("warehouse.write", "warehouse.write"),
        ("party.read", "party.read"),
        ("party.write", "party.write"),
        ("product.read", "product.read"),
        ("product.write", "product.write"),
        ("variant.read", "variant.read"),
        ("variant.write", "variant.write"),
        ("pricelist.read", "pricelist.read"),
        ("pricelist.write", "pricelist.write"),
        ("pricing.read", "pricing.read"),
        ("pricing.calculate", "pricing.calculate"),
        ("purchaseorder.read", "purchaseorder.read"),
        ("purchaseorder.write", "purchaseorder.write"),
        ("goodsreceipt.read", "goodsreceipt.read"),
        ("goodsreceipt.write", "goodsreceipt.write"),
        ("cashbox.read", "cashbox.read"),
        ("cashbox.write", "cashbox.write"),
        ("bank.read", "bank.read"),
        ("bank.write", "bank.write"),
        ("cashledger.read", "cashledger.read"),
        ("payment.read", "payment.read"),
        ("payment.write", "payment.write"),
        ("reports.read", "reports.read"),
        ("admin", "admin")
    );
});

// Add application services
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();
builder.Services.AddScoped<IPartyService, PartyService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IProductVariantService, ProductVariantService>();
builder.Services.AddScoped<IPriceListService, PriceListService>();
builder.Services.AddScoped<IPriceListItemService, PriceListItemService>();
builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<IShipmentInvoicingService, ShipmentInvoicingService>();
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IPartyLedgerService, PartyLedgerService>();
builder.Services.AddScoped<PaymentAllocationService>();

// Returns & Credit Notes services
builder.Services.AddScoped<SalesReturnService>();
builder.Services.AddScoped<PurchaseReturnService>();
builder.Services.AddScoped<CreditNoteService>();

// Part References & Equivalent Search services
builder.Services.AddScoped<PartReferenceService>();
builder.Services.AddScoped<VariantSearchService>();

// Vehicle Fitment services
builder.Services.AddScoped<IVehicleService, VehicleService>();

// Purchase module services
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IGoodsReceiptService, GoodsReceiptService>();

// Cash/Bank module services
builder.Services.AddScoped<ICashboxService, CashboxService>();
builder.Services.AddScoped<IBankAccountService, BankAccountService>();
builder.Services.AddScoped<ICashBankLedgerService, CashBankLedgerService>();

// Reports module services
builder.Services.AddScoped<IReportsService, ReportsService>();

// E-Document module services
builder.Services.AddScoped<IEDocumentService, EDocumentService>();
builder.Services.AddScoped<UblGenerator>();
builder.Services.AddSingleton<EInvoiceProviderRegistry>();
builder.Services.AddSingleton<IEInvoiceProvider, TestEInvoiceProvider>();

// Register providers
var providerRegistry = new EInvoiceProviderRegistry();
providerRegistry.Register(new TestEInvoiceProvider());
builder.Services.AddSingleton(providerRegistry);

// Add services to the container.
builder.Services.AddControllers();

// Add FluentValidation
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "ErpCloud API",
        Version = "v1",
        Description = "Multi-tenant ERP Cloud API with JWT Authentication"
    });

    // Add JWT Bearer authentication to Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT Authorization header using Bearer scheme. Enter your JWT token in the text input below."
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddHealthChecks();

var app = builder.Build();

// Seed demo users in development
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
        // await ErpCloud.Api.Dev.DatabaseSeeder.SeedDemoUsers(context); // Temporarily disabled due to FK constraint issue
    }
}

// Configure CORS for desktop app
app.UseCors(policy =>
{
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader();
});

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();

app.UseSerilogRequestLogging();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Tenant Context Middleware (after auth)
app.UseTenantContext();

// Map controllers
app.MapControllers();

// Health endpoint
app.MapHealthChecks("/health");

app.MapGet("/", () => Results.Ok(new
{
    Application = "ErpCloud API",
    Version = "1.0.0",
    Status = "Running"
}))
.WithName("Root")
.WithOpenApi();

try
{
    Log.Information("Starting ErpCloud API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
