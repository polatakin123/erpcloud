using System.ComponentModel.DataAnnotations;

namespace ErpCloud.Api.Models;

public record UpdateShipmentDto(
    [Required, StringLength(32, MinimumLength = 2), RegularExpression(@"^[A-Z0-9_-]+$")]
    string ShipmentNo,
    
    [Required]
    Guid BranchId,
    
    [Required]
    Guid WarehouseId,
    
    [Required]
    DateTime ShipmentDate,
    
    string? Note,
    
    [Required, MinLength(1)]
    List<UpdateShipmentLineDto> Lines
);

public record UpdateShipmentLineDto(
    [Required]
    Guid SalesOrderLineId,
    
    [Required]
    Guid VariantId,
    
    [Required, Range(0.001, double.MaxValue)]
    decimal Qty,
    
    string? Note
);
