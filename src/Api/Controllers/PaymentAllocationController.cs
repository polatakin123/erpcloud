using ErpCloud.Api.Data;
using ErpCloud.Api.Services;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentAllocationController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly PaymentAllocationService _allocationService;
    private readonly TenantContextAccessor _tenantAccessor;

    public PaymentAllocationController(
        ErpDbContext context,
        PaymentAllocationService allocationService,
        TenantContextAccessor tenantAccessor)
    {
        _context = context;
        _allocationService = allocationService;
        _tenantAccessor = tenantAccessor;
    }

    /// <summary>
    /// Allocate payment to invoices (bulk upsert)
    /// </summary>
    [HttpPost("{paymentId:guid}/allocate")]
    public async Task<IActionResult> AllocatePayment(
        Guid paymentId,
        [FromBody] AllocatePaymentRequest request)
    {
        var result = await _allocationService.AllocatePaymentAsync(
            paymentId, 
            request.Allocations, 
            HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            if (errorCode.Contains("mismatch") || errorCode.Contains("over_allocate") || errorCode == "invalid_amount")
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });
            
            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Payment allocated successfully" });
    }

    /// <summary>
    /// Auto-allocate payment to invoice(s)
    /// Optionally provide specific invoice IDs, otherwise allocates to oldest open invoices
    /// </summary>
    [HttpPost("{paymentId:guid}/auto-allocate")]
    public async Task<IActionResult> AutoAllocatePayment(
        Guid paymentId,
        [FromBody] AutoAllocateRequest? request = null)
    {
        var result = await _allocationService.AutoAllocateAsync(
            paymentId,
            request?.InvoiceIds,
            HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            var errorCode = result.Error.Code;
            
            if (errorCode.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            if (errorCode.Contains("mismatch") || errorCode.Contains("over_allocate") || errorCode == "invalid_amount")
                return Conflict(new { error = result.Error.Code, message = result.Error.Message });
            
            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Get allocations for a payment
    /// </summary>
    [HttpGet("{paymentId:guid}/allocations")]
    public async Task<IActionResult> GetPaymentAllocations(Guid paymentId)
    {
        var allocations = await _allocationService.GetPaymentAllocationsAsync(
            paymentId, 
            HttpContext.RequestAborted);

        var response = allocations.Select(a => new
        {
            a.Id,
            a.InvoiceId,
            InvoiceNo = a.Invoice.InvoiceNo,
            InvoiceType = a.Invoice.Type,
            a.Amount,
            a.Currency,
            a.AllocatedAt,
            a.Note
        });

        return Ok(response);
    }

    /// <summary>
    /// Remove allocation
    /// </summary>
    [HttpDelete("{paymentId:guid}/allocations/{invoiceId:guid}")]
    public async Task<IActionResult> RemoveAllocation(Guid paymentId, Guid invoiceId)
    {
        var result = await _allocationService.RemoveAllocationAsync(
            paymentId, 
            invoiceId, 
            HttpContext.RequestAborted);

        if (!result.IsSuccess)
        {
            if (result.Error.Code.EndsWith("_not_found"))
                return NotFound(new { error = result.Error.Code, message = result.Error.Message });
            
            return StatusCode(500, new { error = result.Error.Code, message = result.Error.Message });
        }

        return Ok(new { message = "Allocation removed successfully" });
    }

    /// <summary>
    /// Get eligible payments for invoice allocation (same party, same currency, has unallocated amount)
    /// </summary>
    [HttpGet("eligible")]
    public async Task<IActionResult> GetEligiblePayments(
        [FromQuery] Guid invoiceId,
        [FromQuery] string? direction = null)
    {
        var tenantId = _tenantAccessor.TenantContext.TenantId;

        // Load invoice to get party and currency
        var invoice = await _context.Invoices
            .FirstOrDefaultAsync(i => i.Id == invoiceId && i.TenantId == tenantId);

        if (invoice == null)
            return NotFound(new { error = "invoice_not_found", message = "Invoice not found" });

        // Determine required payment direction based on invoice type
        var requiredDirection = invoice.Type == "SALES" ? "IN" : "OUT";

        // Get payments with unallocated amount
        var payments = await _context.Payments
            .Where(p => 
                p.TenantId == tenantId &&
                p.PartyId == invoice.PartyId &&
                p.Currency == invoice.Currency &&
                p.Direction == requiredDirection &&
                p.UnallocatedAmount > 0)
            .OrderByDescending(p => p.Date)
            .Select(p => new
            {
                p.Id,
                p.PaymentNo,
                p.Date,
                p.Direction,
                p.Method,
                p.Amount,
                p.AllocatedAmount,
                p.UnallocatedAmount,
                p.Currency
            })
            .ToListAsync();

        return Ok(payments);
    }
}

public class AllocatePaymentRequest
{
    public List<AllocationRequest> Allocations { get; set; } = new();
}

public class AutoAllocateRequest
{
    public List<Guid>? InvoiceIds { get; set; }
}
