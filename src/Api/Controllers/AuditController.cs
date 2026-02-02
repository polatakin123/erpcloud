using ErpCloud.Api.Data;
using ErpCloud.Api.Models;
using ErpCloud.BuildingBlocks.Auditing;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AuditController(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get audit logs with filtering and pagination
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedAuditResponse>> GetAuditLogs(
        [FromQuery] string? entityName = null,
        [FromQuery] Guid? entityId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        var tenantId = _tenantContext.TenantId;

        // Validate pagination
        if (page < 1) page = 1;
        if (size < 1) size = 50;
        if (size > 200) size = 200;

        // Build query with tenant isolation
        var query = _context.Set<AuditLog>()
            .Where(a => a.TenantId == tenantId);

        // Apply filters
        if (!string.IsNullOrWhiteSpace(entityName))
            query = query.Where(a => a.EntityName == entityName);

        if (entityId.HasValue)
            query = query.Where(a => a.EntityId == entityId.Value.ToString());

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.OccurredAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.OccurredAt <= to.Value);

        // Get total count
        var total = await query.CountAsync();

        // Apply pagination and ordering
        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => new AuditLogDto
            {
                Id = a.Id,
                OccurredAt = a.OccurredAt,
                Action = a.Action.ToString(),
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                UserId = a.UserId,
                DiffJson = a.DiffJson,
                CorrelationId = a.CorrelationId
            })
            .ToListAsync();

        return Ok(new PaginatedAuditResponse
        {
            Page = page,
            Size = size,
            Total = total,
            Items = items
        });
    }
}
