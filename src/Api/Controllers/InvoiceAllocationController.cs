using ErpCloud.Api.Data;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/invoices")]
[Authorize]
public class InvoiceAllocationController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly PaymentAllocationService _allocationService;
    private readonly TenantContextAccessor _tenantAccessor;

    public InvoiceAllocationController(
        ErpDbContext context,
        PaymentAllocationService allocationService,
        TenantContextAccessor tenantAccessor)
    {
        _context = context;
        _allocationService = allocationService;
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Get allocations for an invoice
    /// </summary>
    [HttpGet("{invoiceId:guid}/allocations")]
    public async Task<IActionResult> GetInvoiceAllocations(Guid invoiceId)
    {
        var allocations = await _allocationService.GetInvoiceAllocationsAsync(
            invoiceId, 
            HttpContext.RequestAborted);

        var response = allocations.Select(a => new
        {
            a.Id,
            a.PaymentId,
            PaymentNo = a.Payment.PaymentNo,
            PaymentDate = a.Payment.Date,
            PaymentDirection = a.Payment.Direction,
            a.Amount,
            a.Currency,
            a.AllocatedAt,
            a.Note
        });

        return Ok(response);
    }

    /// <summary>
    /// Get eligible invoices for payment allocation (same party, same currency, has open amount)
    /// </summary>
    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligibleInvoices(
        [FromQuery] Guid paymentId)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;

        // Load payment to get party, currency, and direction
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId);

        if (payment == null)
            return NotFound(new { error = "payment_not_found", message = "Payment not found" });

        // Determine required invoice type based on payment direction
        var requiredType = payment.Direction == "IN" ? "SALES" : "PURCHASE";

        // Get invoices with open amount
        var invoices = await _context.Invoices
            .Where(i => 
                i.TenantId == tenantId &&
                i.PartyId == payment.PartyId &&
                i.Currency == payment.Currency &&
                i.Type == requiredType &&
                i.Status == "ISSUED" &&
                i.OpenAmount > 0)
            .OrderBy(i => i.DueDate)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNo,
                i.Type,
                i.IssueDate,
                i.DueDate,
                i.GrandTotal,
                i.PaidAmount,
                i.OpenAmount,
                i.PaymentStatus,
                i.Currency
            })
            .ToListAsync();

        return Ok(invoices);
    }
}
