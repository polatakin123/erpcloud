using ErpCloud.BuildingBlocks.Persistence;

namespace ErpCloud.Api.Entities;

/// <summary>
/// Sales Order header
/// </summary>
public class SalesOrder : TenantEntity
{
    /// <summary>
    /// Primary key
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Order number (unique per tenant)
    /// </summary>
    public string OrderNo { get; set; } = string.Empty;

    /// <summary>
    /// Customer (FK to Party)
    /// </summary>
    public Guid PartyId { get; set; }
    public Party Party { get; set; } = null!;

    /// <summary>
    /// Branch (FK to Branch)
    /// </summary>
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    /// <summary>
    /// Warehouse from which stock will be issued (FK to Warehouse)
    /// </summary>
    public Guid WarehouseId { get; set; }
    public Warehouse Warehouse { get; set; } = null!;

    /// <summary>
    /// Optional price list (FK to PriceList)
    /// </summary>
    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>
    /// Order status: DRAFT | CONFIRMED | COMPLETED | CANCELLED
    /// </summary>
    public string Status { get; set; } = "DRAFT";

    /// <summary>
    /// Order date
    /// </summary>
    public DateTime OrderDate { get; set; }

    /// <summary>
    /// Optional note
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Order lines
    /// </summary>
    public ICollection<SalesOrderLine> Lines { get; set; } = new List<SalesOrderLine>();
}
