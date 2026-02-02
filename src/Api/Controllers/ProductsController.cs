using ErpCloud.Api.Models;
using ErpCloud.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ErpCloud.Api.Controllers;

[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    [Authorize(Policy = "product.write")]
    public async Task<ActionResult<ProductDto>> Create([FromBody] CreateProductDto dto)
    {
        try
        {
            var product = await _productService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpGet]
    [Authorize(Policy = "product.read")]
    public async Task<ActionResult<PaginatedResponse<ProductDto>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? q = null,
        [FromQuery] bool? active = null)
    {
        var result = await _productService.GetAllAsync(page, size, q, active);
        return Ok(result);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "product.read")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null) return NotFound();
        return Ok(product);
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "product.write")]
    public async Task<ActionResult<ProductDto>> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        try
        {
            var product = await _productService.UpdateAsync(id, dto);
            if (product == null) return NotFound();
            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "product.write")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _productService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
