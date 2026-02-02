using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/bank-accounts")]
public class BankAccountController : ControllerBase
{
    private readonly IBankAccountService _bankAccountService;

    public BankAccountController(IBankAccountService bankAccountService)
    {
        _bankAccountService = bankAccountService;
    }

    [HttpPost]
    public async Task<ActionResult<BankAccountDto>> Create([FromBody] CreateBankAccountDto dto)
    {
        try
        {
            var account = await _bankAccountService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = account.Id }, account);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<BankAccountListDto>>> Search([FromQuery] BankAccountSearchDto dto)
    {
        var result = await _bankAccountService.SearchAsync(dto);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BankAccountDto>> GetById(Guid id)
    {
        var account = await _bankAccountService.GetByIdAsync(id);
        if (account == null)
        {
            return NotFound(new { error = "not_found", message = "Bank account not found" });
        }
        return Ok(account);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<BankAccountDto>> Update(Guid id, [FromBody] UpdateBankAccountDto dto)
    {
        try
        {
            var account = await _bankAccountService.UpdateAsync(id, dto);
            return Ok(account);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _bankAccountService.DeleteAsync(id);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = "validation_failed", message = ex.Message });
        }
    }
}
