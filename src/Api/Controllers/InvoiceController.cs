using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/invoices")]
public class InvoiceController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoiceController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> CreateDraft([FromBody] CreateInvoiceDto dto)
    {
        var result = await _invoiceService.CreateDraftAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<InvoiceDto>> UpdateDraft(Guid id, [FromBody] UpdateInvoiceDto dto)
    {
        var result = await _invoiceService.UpdateDraftAsync(id, dto);
        return Ok(result);
    }

    [HttpPost("{id}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(Guid id)
    {
        var result = await _invoiceService.IssueAsync(id);
        return Ok(result);
    }

    [HttpPost("{id}/cancel")]
    public async Task<ActionResult<InvoiceDto>> Cancel(Guid id)
    {
        var result = await _invoiceService.CancelAsync(id);
        return Ok(result);
    }

    [HttpGet("{id}", Name = "GetInvoice")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id)
    {
        var result = await _invoiceService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<InvoiceListDto>> Search([FromQuery] InvoiceSearchDto search)
    {
        var result = await _invoiceService.SearchAsync(search);
        return Ok(result);
    }
}
