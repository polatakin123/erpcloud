using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Services;

public interface IEDocumentService
{
    Task<EDocumentDto> CreateAsync(CreateEDocumentDto dto);
    Task<EDocumentDto?> GetByIdAsync(Guid id);
    Task<EDocumentWithHistoryDto?> GetByIdWithHistoryAsync(Guid id);
    Task<PagedResult<EDocumentDto>> SearchAsync(EDocumentQuery query);
    Task<EDocumentDto> RetryAsync(Guid id);
    Task<EDocumentDto> CancelAsync(Guid id);
}

public class EDocumentService : IEDocumentService
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public EDocumentService(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<EDocumentDto> CreateAsync(CreateEDocumentDto dto)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId ?? Guid.Empty;

        // Validate invoice exists and is ISSUED
        var invoice = await _context.Set<Invoice>()
            .FirstOrDefaultAsync(i => i.Id == dto.InvoiceId && i.TenantId == tenantId);

        if (invoice == null)
        {
            throw new InvalidOperationException("Invoice not found");
        }

        if (invoice.Status != "ISSUED")
        {
            throw new InvalidOperationException($"Invoice must be ISSUED to create e-document. Current status: {invoice.Status}");
        }

        // Check idempotency: if already exists, return existing
        var existing = await _context.Set<EDocument>()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId 
                && e.InvoiceId == dto.InvoiceId 
                && e.DocumentType == dto.DocumentType.ToUpperInvariant());

        if (existing != null)
        {
            return MapToDto(existing);
        }

        // Create new e-document
        var eDocument = new EDocument
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            InvoiceId = dto.InvoiceId,
            DocumentType = dto.DocumentType.ToUpperInvariant(),
            Scenario = (dto.Scenario ?? "BASIC").ToUpperInvariant(),
            Status = "DRAFT",
            ProviderCode = "TEST", // Default to TEST provider
            Uuid = Guid.NewGuid(),
            RetryCount = 0,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId
        };

        _context.Set<EDocument>().Add(eDocument);

        // Add initial status history
        var history = new EDocumentStatusHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EDocumentId = eDocument.Id,
            Status = "DRAFT",
            Message = "E-Document created",
            OccurredAt = DateTime.UtcNow
        };

        _context.Set<EDocumentStatusHistory>().Add(history);

        // TODO: Write outbox event for async processing when Outbox module is ready
        // For now, document will stay in DRAFT status until Worker is implemented

        await _context.SaveChangesAsync();

        return MapToDto(eDocument);
    }

    public async Task<EDocumentDto?> GetByIdAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var eDocument = await _context.Set<EDocument>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        return eDocument == null ? null : MapToDto(eDocument);
    }

    public async Task<EDocumentWithHistoryDto?> GetByIdWithHistoryAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var eDocument = await _context.Set<EDocument>()
            .Include(e => e.StatusHistory)
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (eDocument == null)
        {
            return null;
        }

        return new EDocumentWithHistoryDto(
            eDocument.Id,
            eDocument.TenantId,
            eDocument.InvoiceId,
            eDocument.DocumentType,
            eDocument.Scenario,
            eDocument.Status,
            eDocument.ProviderCode,
            eDocument.Uuid,
            eDocument.EnvelopeId,
            eDocument.GIBReference,
            eDocument.LastStatusMessage,
            eDocument.RetryCount,
            eDocument.LastTriedAt,
            eDocument.CreatedAt,
            eDocument.CreatedBy,
            eDocument.StatusHistory
                .OrderByDescending(h => h.OccurredAt)
                .Select(h => new EDocumentStatusHistoryDto(h.Id, h.EDocumentId, h.Status, h.Message, h.OccurredAt))
                .ToList()
        );
    }

    public async Task<PagedResult<EDocumentDto>> SearchAsync(EDocumentQuery query)
    {
        var tenantId = _tenantContext.TenantId;

        var q = _context.Set<EDocument>()
            .Where(e => e.TenantId == tenantId);

        if (query.InvoiceId.HasValue)
        {
            q = q.Where(e => e.InvoiceId == query.InvoiceId.Value);
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            q = q.Where(e => e.Status == query.Status.ToUpperInvariant());
        }

        if (!string.IsNullOrEmpty(query.DocumentType))
        {
            q = q.Where(e => e.DocumentType == query.DocumentType.ToUpperInvariant());
        }

        var total = await q.CountAsync();

        var items = await q
            .OrderByDescending(e => e.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResult<EDocumentDto>(
            Items: items.Select(MapToDto).ToList(),
            TotalCount: total,
            Page: query.Page,
            PageSize: query.PageSize
        );
    }

    public async Task<EDocumentDto> RetryAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var eDocument = await _context.Set<EDocument>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (eDocument == null)
        {
            throw new InvalidOperationException("E-Document not found");
        }

        if (eDocument.Status != "ERROR")
        {
            throw new InvalidOperationException($"Can only retry ERROR documents. Current status: {eDocument.Status}");
        }

        if (eDocument.RetryCount >= 3)
        {
            throw new InvalidOperationException("Maximum retry count reached (3)");
        }

        // Reset status to DRAFT for retry
        eDocument.Status = "DRAFT";

        // Add status history
        var history = new EDocumentStatusHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EDocumentId = eDocument.Id,
            Status = "DRAFT",
            Message = $"Retry attempt #{eDocument.RetryCount + 1}",
            OccurredAt = DateTime.UtcNow
        };

        _context.Set<EDocumentStatusHistory>().Add(history);

        // TODO: Write outbox event for retry when Outbox module is ready

        await _context.SaveChangesAsync();

        return MapToDto(eDocument);
    }

    public async Task<EDocumentDto> CancelAsync(Guid id)
    {
        var tenantId = _tenantContext.TenantId;

        var eDocument = await _context.Set<EDocument>()
            .FirstOrDefaultAsync(e => e.Id == id && e.TenantId == tenantId);

        if (eDocument == null)
        {
            throw new InvalidOperationException("E-Document not found");
        }

        // Can only cancel if not SENT or ACCEPTED
        if (eDocument.Status == "SENT" || eDocument.Status == "ACCEPTED")
        {
            throw new InvalidOperationException($"Cannot cancel document in status {eDocument.Status}");
        }

        eDocument.Status = "CANCELLED";

        // Add status history
        var history = new EDocumentStatusHistory
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EDocumentId = eDocument.Id,
            Status = "CANCELLED",
            Message = "Document cancelled by user",
            OccurredAt = DateTime.UtcNow
        };

        _context.Set<EDocumentStatusHistory>().Add(history);

        await _context.SaveChangesAsync();

        return MapToDto(eDocument);
    }

    private static EDocumentDto MapToDto(EDocument e)
    {
        return new EDocumentDto(
            e.Id,
            e.TenantId,
            e.InvoiceId,
            e.DocumentType,
            e.Scenario,
            e.Status,
            e.ProviderCode,
            e.Uuid,
            e.EnvelopeId,
            e.GIBReference,
            e.LastStatusMessage,
            e.RetryCount,
            e.LastTriedAt,
            e.CreatedAt,
            e.CreatedBy
        );
    }
}
