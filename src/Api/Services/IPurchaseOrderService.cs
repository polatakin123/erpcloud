using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

public interface IPurchaseOrderService
{
    Task<PurchaseOrderDto> CreateDraftAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdateDraftAsync(Guid id, UpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> ConfirmAsync(Guid id);
    Task<PurchaseOrderDto> CancelAsync(Guid id);
    Task<PurchaseOrderDto> GetByIdAsync(Guid id);
    Task<PagedResult<PurchaseOrderListDto>> SearchAsync(PurchaseOrderSearchDto dto);
}
