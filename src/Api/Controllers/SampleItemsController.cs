using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/sample-items")]
[Authorize]
public class SampleItemsController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly ITenantContext _tenantContext;

    public SampleItemsController(ErpDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Get all sample items (audit log test)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<SampleItem>>> GetAll()
    {
        var items = await _context.Set<SampleItem>().ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Create sample item (triggers Created audit log)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSampleItemRequest request)
    {
        var item = new SampleItem
        {
            Id = Guid.NewGuid(),
            TenantId = _tenantContext.TenantId,
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Password = request.Password,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = _tenantContext.UserId ?? Guid.Empty
        };

        _context.Set<SampleItem>().Add(item);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetAll), new { id = item.Id }, item);
    }

    /// <summary>
    /// Update sample item (triggers Updated audit log with diff)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSampleItemRequest request)
    {
        var item = await _context.Set<SampleItem>().FindAsync(id);
        
        if (item == null)
            return NotFound();

        // Update only provided fields
        if (request.Name != null) item.Name = request.Name;
        if (request.Description != null) item.Description = request.Description;
        if (request.Price.HasValue) item.Price = request.Price.Value;
        if (request.Password != null) item.Password = request.Password;

        await _context.SaveChangesAsync();

        return Ok(item);
    }

    /// <summary>
    /// Delete sample item (triggers Deleted audit log)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var item = await _context.Set<SampleItem>().FindAsync(id);
        
        if (item == null)
            return NotFound();

        _context.Set<SampleItem>().Remove(item);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateSampleItemRequest(string Name, string? Description, decimal Price, string? Password);
public record UpdateSampleItemRequest(string? Name, string? Description, decimal? Price, string? Password);
