using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/stock")]
[Authorize]
public class StockController : ControllerBase
{
    private readonly IStockService _stockService;

    public StockController(IStockService stockService)
    {
        _stockService = stockService;
    }

    /// <summary>
    /// Get stock balances with optional filters
    /// </summary>
    [HttpGet("balances")]
    [Authorize(Policy = "stock.read")]
    public async Task<ActionResult<PaginatedResponse<StockBalanceDto>>> GetBalances(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? variantId = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        var result = await _stockService.GetBalancesAsync(warehouseId, variantId, page, size);
        return Ok(result);
    }

    /// <summary>
    /// Get specific balance for warehouse and variant
    /// </summary>
    [HttpGet("balances/{warehouseId}/{variantId}")]
    [Authorize(Policy = "stock.read")]
    public async Task<ActionResult<StockBalanceDto>> GetBalance(Guid warehouseId, Guid variantId)
    {
        var balance = await _stockService.GetBalanceAsync(warehouseId, variantId);
        return Ok(balance);
    }

    /// <summary>
    /// Get stock ledger entries with optional filters
    /// </summary>
    [HttpGet("ledger")]
    [Authorize(Policy = "stock.read")]
    public async Task<ActionResult<PaginatedResponse<StockLedgerDto>>> GetLedger(
        [FromQuery] Guid? warehouseId = null,
        [FromQuery] Guid? variantId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int size = 50)
    {
        var result = await _stockService.GetLedgerAsync(warehouseId, variantId, from, to, page, size);
        return Ok(result);
    }

    /// <summary>
    /// Receive stock (INBOUND movement)
    /// </summary>
    [HttpPost("receive")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<StockLedgerDto>> ReceiveStock([FromBody] ReceiveStockDto dto)
    {
        try
        {
            var entry = await _stockService.ReceiveStockAsync(dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Issue stock (OUTBOUND movement)
    /// </summary>
    [HttpPost("issue")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<StockLedgerDto>> IssueStock([FromBody] IssueStockDto dto)
    {
        try
        {
            var entry = await _stockService.IssueStockAsync(dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Reserve stock for an order
    /// </summary>
    [HttpPost("reserve")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<StockLedgerDto>> ReserveStock([FromBody] ReserveStockDto dto)
    {
        try
        {
            var entry = await _stockService.ReserveStockAsync(dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Release stock reservation
    /// </summary>
    [HttpPost("release")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<StockLedgerDto>> ReleaseReservation([FromBody] ReleaseReservationDto dto)
    {
        try
        {
            var entry = await _stockService.ReleaseReservationAsync(dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Transfer stock between warehouses
    /// </summary>
    [HttpPost("transfer")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<object>> TransferStock([FromBody] TransferStockDto dto)
    {
        try
        {
            var (outEntry, inEntry) = await _stockService.TransferStockAsync(dto);
            return Ok(new { transferOut = outEntry, transferIn = inEntry });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Adjust stock (stock count adjustment)
    /// </summary>
    [HttpPost("adjust")]
    [Authorize(Policy = "stock.write")]
    public async Task<ActionResult<StockLedgerDto>> AdjustStock([FromBody] AdjustStockDto dto)
    {
        try
        {
            var entry = await _stockService.AdjustStockAsync(dto);
            return Ok(entry);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
