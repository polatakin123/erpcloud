using ErpCloud.Api.Data;
using ErpCloud.Api.Entities;
using ErpCloud.Api.Events;
using ErpCloud.BuildingBlocks.Outbox;
using ErpCloud.BuildingBlocks.Tenant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("demo")]
[Authorize]
public class DemoController : ControllerBase
{
    private readonly ErpDbContext _context;
    private readonly ILogger<DemoController> _logger;
    private readonly IOutboxWriter _outboxWriter;
    private readonly ITenantContext _tenantContext;

    public DemoController(
        ErpDbContext context, 
        ILogger<DemoController> logger,
        IOutboxWriter outboxWriter,
        ITenantContext tenantContext)
    {
        _context = context;
        _logger = logger;
        _outboxWriter = outboxWriter;
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Create a demo item (triggers audit log creation)
    /// </summary>
    [HttpPost("items")]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        var item = new DemoItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        _context.Set<DemoItem>().Add(item);
        await _context.SaveChangesAsync();

        return Ok(new { Id = item.Id, Message = "Item created with audit log" });
    }

    /// <summary>
    /// Update a demo item (triggers audit log with diff)
    /// </summary>
    [HttpPut("items/{id}")]
    public async Task<IActionResult> UpdateItem(Guid id, [FromBody] UpdateItemRequest request)
    {
        var item = await _context.Set<DemoItem>().FindAsync(id);
        
        if (item == null)
        {
            return NotFound();
        }

        item.Name = request.Name ?? item.Name;
        item.Description = request.Description ?? item.Description;
        item.Price = request.Price ?? item.Price;

        await _context.SaveChangesAsync();

        return Ok(new { Id = item.Id, Message = "Item updated with audit log" });
    }

    /// <summary>
    /// Delete a demo item (triggers audit log)
    /// </summary>
    [HttpDelete("items/{id}")]
    public async Task<IActionResult> DeleteItem(Guid id)
    {
        var item = await _context.Set<DemoItem>().FindAsync(id);
        
        if (item == null)
        {
            return NotFound();
        }

        _context.Set<DemoItem>().Remove(item);
        await _context.SaveChangesAsync();

        return Ok(new { Message = "Item deleted with audit log" });
    }

    /// <summary>
    /// Get all demo items
    /// </summary>
    [HttpGet("items")]
    public async Task<IActionResult> GetItems()
    {
        var items = await _context.Set<DemoItem>().ToListAsync();
        return Ok(items);
    }

    /// <summary>
    /// Demo endpoint to test transactional outbox pattern
    /// </summary>
    [HttpPost("publish")]
    public async Task<IActionResult> PublishDemoEvent([FromBody] PublishDemoRequest request)
    {
        var tenantId = _tenantContext.TenantId;

        var demoEvent = new DemoEventCreated
        {
            OrderNo = request.OrderNo,
            Amount = request.Amount,
            Timestamp = DateTime.UtcNow
        };

        // Add to outbox (will be persisted in same transaction as business logic)
        await _outboxWriter.AddEventAsync(tenantId, demoEvent);

        return Ok(new
        {
            TenantId = tenantId,
            OrderNo = request.OrderNo,
            Amount = request.Amount,
            Message = "Event enqueued to outbox"
        });
    }
}

public record CreateItemRequest(string Name, string? Description, decimal Price);
public record UpdateItemRequest(string? Name, string? Description, decimal? Price);
public record PublishDemoRequest(string OrderNo, decimal Amount);
