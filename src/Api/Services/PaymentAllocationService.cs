using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Common;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

/// <summary>
/// Manages payment-to-invoice allocations
/// Handles partial payments, cache updates, and business rules
/// </summary>
public class PaymentAllocationService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PaymentAllocationService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Allocate payment to one or more invoices
    /// Idempotent: if allocation already exists, updates the amount
    /// </summary>
    public async Task<Result> AllocatePaymentAsync(
        Guid paymentId, 
        List<AllocationRequest> allocations,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        // Start transaction for consistency
        using var transaction = await _context.Database.BeginTransactionAsync(ct);
        
        try
        {
            // 1. Load payment with lock (re-read to avoid stale data)
            var payment = await _context.Payments
                .Include(p => p.Allocations)
                .FirstOrDefaultAsync(p => p.Id == paymentId && p.TenantId == tenantId, ct);

            if (payment == null)
                return Result.Failure(Error.NotFound("payment_not_found", $"Payment {paymentId} not found"));

            // 2. Validate and process each allocation
            foreach (var request in allocations)
            {
                // Load invoice with lock
                var invoice = await _context.Invoices
                    .Include(i => i.Allocations)
                    .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && i.TenantId == tenantId, ct);

                if (invoice == null)
                    return Result.Failure(Error.NotFound("invoice_not_found", $"Invoice {request.InvoiceId} not found"));

                // Business rule validations
                var validationResult = ValidateAllocation(payment, invoice, request.Amount);
                if (!validationResult.IsSuccess)
                    return validationResult;

                // Upsert allocation (idempotent)
                var existingAllocation = payment.Allocations
                    .FirstOrDefault(a => a.InvoiceId == request.InvoiceId);

                if (existingAllocation != null)
                {
                    // Update existing allocation
                    var oldAmount = existingAllocation.Amount;
                    var delta = request.Amount - oldAmount;

                    // Validate new amount doesn't over-allocate
                    if (delta > 0)
                    {
                        if (payment.UnallocatedAmount < delta)
                            return Result.Failure(Error.Conflict("over_allocate_payment", 
                                $"Payment {payment.PaymentNo} only has {payment.UnallocatedAmount:N2} {payment.Currency} unallocated"));

                        if (invoice.OpenAmount < delta)
                            return Result.Failure(Error.Conflict("over_allocate_invoice", 
                                $"Invoice {invoice.InvoiceNo} only has {invoice.OpenAmount:N2} {invoice.Currency} open"));
                    }

                    existingAllocation.Amount = request.Amount;
                    existingAllocation.AllocatedAt = DateTime.UtcNow;
                    existingAllocation.Note = request.Note;

                    // Update caches with delta
                    payment.AllocatedAmount += delta;
                    payment.UnallocatedAmount -= delta;
                    invoice.PaidAmount += delta;
                    invoice.OpenAmount -= delta;
                }
                else
                {
                    // Create new allocation
                    if (payment.UnallocatedAmount < request.Amount)
                        return Result.Failure(Error.Conflict("over_allocate_payment", 
                            $"Payment {payment.PaymentNo} only has {payment.UnallocatedAmount:N2} {payment.Currency} unallocated"));

                    if (invoice.OpenAmount < request.Amount)
                        return Result.Failure(Error.Conflict("over_allocate_invoice", 
                            $"Invoice {invoice.InvoiceNo} only has {invoice.OpenAmount:N2} {invoice.Currency} open"));

                    var allocation = new PaymentAllocation
                    {
                        Id = Guid.NewGuid(),
                        TenantId = tenantId,
                        PartyId = payment.PartyId,
                        InvoiceId = invoice.Id,
                        PaymentId = payment.Id,
                        Currency = payment.Currency,
                        Amount = request.Amount,
                        AllocatedAt = DateTime.UtcNow,
                        Note = request.Note
                    };

                    _context.PaymentAllocations.Add(allocation);

                    // Update caches
                    payment.AllocatedAmount += request.Amount;
                    payment.UnallocatedAmount -= request.Amount;
                    invoice.PaidAmount += request.Amount;
                    invoice.OpenAmount -= request.Amount;
                }

                // Update invoice payment status
                UpdateInvoicePaymentStatus(invoice);
            }

            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Unexpected("allocation_failed", ex.Message));
        }
    }

    /// <summary>
    /// Remove allocation between payment and invoice
    /// </summary>
    public async Task<Result> RemoveAllocationAsync(
        Guid paymentId, 
        Guid invoiceId,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        using var transaction = await _context.Database.BeginTransactionAsync(ct);

        try
        {
            var allocation = await _context.PaymentAllocations
                .FirstOrDefaultAsync(a => 
                    a.PaymentId == paymentId && 
                    a.InvoiceId == invoiceId && 
                    a.TenantId == tenantId, ct);

            if (allocation == null)
                return Result.Failure(Error.NotFound("allocation_not_found", "Allocation not found"));

            // Load payment and invoice to update caches
            var payment = await _context.Payments.FindAsync(new object[] { paymentId }, ct);
            var invoice = await _context.Invoices.FindAsync(new object[] { invoiceId }, ct);

            if (payment == null || invoice == null)
                return Result.Failure(Error.NotFound("entity_not_found", "Payment or invoice not found"));

            // Restore cache values
            payment.AllocatedAmount -= allocation.Amount;
            payment.UnallocatedAmount += allocation.Amount;
            invoice.PaidAmount -= allocation.Amount;
            invoice.OpenAmount += allocation.Amount;

            // Update invoice payment status
            UpdateInvoicePaymentStatus(invoice);

            _context.PaymentAllocations.Remove(allocation);
            await _context.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return Result.Failure(Error.Unexpected("remove_allocation_failed", ex.Message));
        }
    }

    /// <summary>
    /// Get allocations for a payment
    /// </summary>
    public async Task<List<PaymentAllocation>> GetPaymentAllocationsAsync(
        Guid paymentId,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _context.PaymentAllocations
            .Include(a => a.Invoice)
            .Include(a => a.Payment)
            .Where(a => a.PaymentId == paymentId && a.TenantId == tenantId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Get allocations for an invoice
    /// </summary>
    public async Task<List<PaymentAllocation>> GetInvoiceAllocationsAsync(
        Guid invoiceId,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;

        return await _context.PaymentAllocations
            .Include(a => a.Payment)
            .Include(a => a.Invoice)
            .Where(a => a.InvoiceId == invoiceId && a.TenantId == tenantId)
            .OrderByDescending(a => a.AllocatedAt)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Validate allocation business rules
    /// </summary>
    private Result ValidateAllocation(Payment payment, Invoice invoice, decimal amount)
    {
        // Amount must be positive
        if (amount <= 0)
            return Result.Failure(Error.Validation("invalid_amount", "Amount must be positive"));

        // Party must match
        if (payment.PartyId != invoice.PartyId)
            return Result.Failure(Error.Conflict("party_mismatch", 
                $"Payment party {payment.PartyId} doesn't match invoice party {invoice.PartyId}"));

        // Currency must match
        if (payment.Currency != invoice.Currency)
            return Result.Failure(Error.Conflict("currency_mismatch", 
                $"Payment currency {payment.Currency} doesn't match invoice currency {invoice.Currency}"));

        // Direction/Type compatibility
        // Payment IN → only SALES invoices
        // Payment OUT → only PURCHASE invoices
        var isValidDirection = (payment.Direction == "IN" && invoice.Type == "SALES") ||
                              (payment.Direction == "OUT" && invoice.Type == "PURCHASE");

        if (!isValidDirection)
            return Result.Failure(Error.Conflict("direction_type_mismatch", 
                $"Payment direction {payment.Direction} cannot be allocated to {invoice.Type} invoice"));

        return Result.Success();
    }

    /// <summary>
    /// Update invoice payment status based on PaidAmount vs GrandTotal
    /// </summary>
    private void UpdateInvoicePaymentStatus(Invoice invoice)
    {
        const decimal tolerance = 0.01m;

        if (invoice.PaidAmount <= tolerance)
        {
            invoice.PaymentStatus = "OPEN";
        }
        else if (Math.Abs(invoice.PaidAmount - invoice.GrandTotal) <= tolerance)
        {
            invoice.PaymentStatus = "PAID";
        }
        else
        {
            invoice.PaymentStatus = "PARTIAL";
        }
    }
}

/// <summary>
/// Request DTO for allocation
/// </summary>
public class AllocationRequest
{
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}
