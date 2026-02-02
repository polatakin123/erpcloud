using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/party-ledger")]
public class PartyLedgerController : ControllerBase
{
    private readonly IPartyLedgerService _partyLedgerService;

    public PartyLedgerController(IPartyLedgerService partyLedgerService)
    {
        _partyLedgerService = partyLedgerService;
    }

    [HttpGet("{partyId}/ledger")]
    public async Task<ActionResult<PartyLedgerListDto>> GetLedger(
        Guid partyId, 
        [FromQuery] PartyLedgerSearchDto search)
    {
        var result = await _partyLedgerService.GetLedgerAsync(partyId, search);
        return Ok(result);
    }

    [HttpGet("{partyId}/balance")]
    public async Task<ActionResult<PartyBalanceDto>> GetBalance(
        Guid partyId, 
        [FromQuery] DateTime? at = null)
    {
        var result = await _partyLedgerService.GetBalanceAsync(partyId, at);
        return Ok(result);
    }
}
