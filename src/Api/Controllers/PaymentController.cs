using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/payments")]
public class PaymentController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PaymentDto>> Create([FromBody] CreatePaymentDto dto)
    {
        var result = await _paymentService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id)
    {
        var result = await _paymentService.GetByIdAsync(id);
        if (result == null)
        {
            return NotFound();
        }
        return Ok(result);
    }

    [HttpGet]
    public async Task<ActionResult<PaymentListDto>> Search([FromQuery] PaymentSearchDto search)
    {
        var result = await _paymentService.SearchAsync(search);
        return Ok(result);
    }
}
