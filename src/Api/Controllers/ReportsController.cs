using ErpCloud.Api.Reports.Dtos;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Policy = "reports.read")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _service;

    public ReportsController(IReportsService service)
    {
        _service = service;
    }

    /// <summary>
    /// Get stock balance summary for a warehouse
    /// </summary>
    [HttpGet("stock/balances")]
    public async Task<ActionResult<PagedReportResult<StockBalanceDto>>> GetStockBalances(
        [FromQuery] Guid warehouseId,
        [FromQuery] string? q,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        if (warehouseId == Guid.Empty)
            return BadRequest(new { error = "warehouseId is required" });

        if (size > 200)
            size = 200;

        var result = await _service.GetStockBalancesAsync(warehouseId, q, page, size);
        return Ok(result);
    }

    /// <summary>
    /// Get stock movements (ledger entries)
    /// </summary>
    [HttpGet("stock/movements")]
    public async Task<ActionResult<PagedReportResult<StockMovementDto>>> GetStockMovements(
        [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? variantId,
        [FromQuery] string? movementType,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        if (size > 200)
            size = 200;

        var result = await _service.GetStockMovementsAsync(warehouseId, variantId, movementType, from, to, page, size);
        return Ok(result);
    }

    /// <summary>
    /// Get sales summary grouped by day or month
    /// </summary>
    [HttpGet("sales/summary")]
    public async Task<ActionResult<List<SalesSummaryDto>>> GetSalesSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string groupBy = "DAY")
    {
        var result = await _service.GetSalesSummaryAsync(from, to, groupBy);
        return Ok(result);
    }

    /// <summary>
    /// Get purchase summary grouped by day or month
    /// </summary>
    [HttpGet("purchase/summary")]
    public async Task<ActionResult<List<SalesSummaryDto>>> GetPurchaseSummary(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string groupBy = "DAY")
    {
        var result = await _service.GetPurchaseSummaryAsync(from, to, groupBy);
        return Ok(result);
    }

    /// <summary>
    /// Get party balance list
    /// </summary>
    [HttpGet("parties/balances")]
    public async Task<ActionResult<PagedReportResult<PartyBalanceDto>>> GetPartyBalances(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] DateTime? at = null)
    {
        if (size > 200)
            size = 200;

        var result = await _service.GetPartyBalancesAsync(q, type, page, size, at);
        return Ok(result);
    }

    /// <summary>
    /// Get party aging report
    /// NOTE: This report shows gross exposure from SALES ISSUED invoices only.
    /// Payment matching is not implemented yet, so aging reflects outstanding invoices.
    /// </summary>
    [HttpGet("parties/aging")]
    public async Task<ActionResult<PagedReportResult<PartyAgingDto>>> GetPartyAging(
        [FromQuery] string? q,
        [FromQuery] string? type,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50,
        [FromQuery] DateTime? at = null)
    {
        if (size > 200)
            size = 200;

        var result = await _service.GetPartyAgingAsync(q, type, page, size, at);
        return Ok(result);
    }

    /// <summary>
    /// Get cash and bank account balances
    /// </summary>
    [HttpGet("cashbank/balances")]
    public async Task<ActionResult<List<CashBankBalanceDto>>> GetCashBankBalances(
        [FromQuery] DateTime? at = null)
    {
        var result = await _service.GetCashBankBalancesAsync(at);
        return Ok(result);
    }
}
