using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/goods-receipts")]
[Authorize]
public class GoodsReceiptController : ControllerBase
{
    private readonly IGoodsReceiptService _service;

    public GoodsReceiptController(IGoodsReceiptService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<GoodsReceiptDto>> Create([FromBody] CreateGoodsReceiptDto dto)
    {
        try
        {
            var result = await _service.CreateDraftAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<GoodsReceiptListDto>>> Search([FromQuery] GoodsReceiptSearchDto dto)
    {
        var result = await _service.SearchAsync(dto);
        return Ok(result);
    }

    [HttpGet("{id}", Name = "GetGoodsReceipt")]
    public async Task<ActionResult<GoodsReceiptDto>> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return NotFound(new { error = "not_found", message = "Goods receipt not found" });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<GoodsReceiptDto>> Update(Guid id, [FromBody] UpdateGoodsReceiptDto dto)
    {
        try
        {
            var result = await _service.UpdateDraftAsync(id, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpPost("{id}/receive")]
    public async Task<ActionResult<GoodsReceiptDto>> Receive(Guid id)
    {
        try
        {
            var result = await _service.ReceiveAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<GoodsReceiptDto>> Cancel(Guid id)
    {
        try
        {
            var result = await _service.CancelAsync(id);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "conflict", message = ex.Message });
        }
    }
}
