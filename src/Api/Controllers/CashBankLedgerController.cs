using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/cash-bank-ledger")]
public class CashBankLedgerController : ControllerBase
{
    private readonly ICashBankLedgerService _ledgerService;

    public CashBankLedgerController(ICashBankLedgerService ledgerService)
    {
        _ledgerService = ledgerService;
    }

    [HttpGet("entries")]
    public async Task<ActionResult<PagedResult<CashBankLedgerDto>>> GetLedger([FromQuery] CashBankLedgerSearchDto dto)
    {
        var result = await _ledgerService.GetLedgerAsync(dto);
        return Ok(result);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<CashBankBalanceDto>> GetBalance([FromQuery] CashBankBalanceQueryDto dto)
    {
        try
        {
            var balance = await _ledgerService.GetBalanceAsync(dto);
            return Ok(balance);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = "validation_failed", message = ex.Message });
        }
    }
}
