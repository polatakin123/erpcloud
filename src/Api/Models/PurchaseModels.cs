using System.ComponentModel.DataAnnotations;

namespace ErpCloud.Api.Models;

// ==================== PurchaseOrder DTOs ====================

public class CreatePurchaseOrderDto
{
    [Required, StringLength(32, MinimumLength = 2)]
    public string PoNo { get; set; } = null!;

    [Required]
    public Guid PartyId { get; set; }

    [Required]
    public Guid BranchId { get; set; }

    [Required]
    public Guid WarehouseId { get; set; }

    [Required]
    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Required, MinLength(1)]
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class UpdatePurchaseOrderDto
{
    [Required]
    public DateOnly OrderDate { get; set; }

    public DateOnly? ExpectedDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Required, MinLength(1)]
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
}

public class PurchaseOrderLineDto
{
    [Required]
    public Guid VariantId { get; set; }

    [Required, Range(0.001, double.MaxValue)]
    public decimal Qty { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }

    [Range(0, 100)]
    public decimal? VatRate { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PoNo { get; set; } = null!;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = null!;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly OrderDate { get; set; }
    public DateOnly? ExpectedDate { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public List<PurchaseOrderLineDetailDto> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseOrderLineDetailDto
{
    public Guid Id { get; set; }
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = null!;
    public string VariantSku { get; set; } = null!;
    public decimal Qty { get; set; }
    public decimal ReceivedQty { get; set; }
    public decimal RemainingQty { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal? VatRate { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
}

public class PurchaseOrderListDto
{
    public Guid Id { get; set; }
    public string PoNo { get; set; } = null!;
    public string PartyName { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== GoodsReceipt DTOs ====================

public class CreateGoodsReceiptDto
{
    [Required, StringLength(32, MinimumLength = 2)]
    public string GrnNo { get; set; } = null!;

    [Required]
    public Guid PurchaseOrderId { get; set; }

    [Required]
    public DateOnly ReceiptDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Required, MinLength(1)]
    public List<GoodsReceiptLineDto> Lines { get; set; } = new();
}

public class UpdateGoodsReceiptDto
{
    [Required]
    public DateOnly ReceiptDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    [Required, MinLength(1)]
    public List<GoodsReceiptLineDto> Lines { get; set; } = new();
}

public class GoodsReceiptLineDto
{
    [Required]
    public Guid PurchaseOrderLineId { get; set; }

    [Required, Range(0.001, double.MaxValue)]
    public decimal Qty { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? UnitCost { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }
}

public class GoodsReceiptDto
{
    public Guid Id { get; set; }
    public string GrnNo { get; set; } = null!;
    public Guid PurchaseOrderId { get; set; }
    public string PoNo { get; set; } = null!;
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = null!;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }
    public List<GoodsReceiptLineDetailDto> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class GoodsReceiptLineDetailDto
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderLineId { get; set; }
    public Guid VariantId { get; set; }
    public string VariantName { get; set; } = null!;
    public string VariantSku { get; set; } = null!;
    public decimal Qty { get; set; }
    public decimal? UnitCost { get; set; }
    public decimal LineTotal { get; set; }
    public string? Note { get; set; }
}

public class GoodsReceiptListDto
{
    public Guid Id { get; set; }
    public string GrnNo { get; set; } = null!;
    public string PoNo { get; set; } = null!;
    public string WarehouseName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly ReceiptDate { get; set; }
    public decimal TotalAmount { get; set; }
    public int LineCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ==================== Search/Pagination ====================

public class PurchaseOrderSearchDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string? Q { get; set; }
    public string? Status { get; set; }
    public Guid? PartyId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}

public class GoodsReceiptSearchDto
{
    public int Page { get; set; } = 1;
    public int Size { get; set; } = 20;
    public string? Q { get; set; }
    public string? Status { get; set; }
    public Guid? PoId { get; set; }
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }
}
