using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

public interface IGoodsReceiptService
{
    Task<GoodsReceiptDto> CreateDraftAsync(CreateGoodsReceiptDto dto);
    Task<GoodsReceiptDto> UpdateDraftAsync(Guid id, UpdateGoodsReceiptDto dto);
    Task<GoodsReceiptDto> ReceiveAsync(Guid id);
    Task<GoodsReceiptDto> CancelAsync(Guid id);
    Task<GoodsReceiptDto> GetByIdAsync(Guid id);
    Task<PagedResult<GoodsReceiptListDto>> SearchAsync(GoodsReceiptSearchDto dto);
}
