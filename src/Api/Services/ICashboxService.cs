using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

public interface ICashboxService
{
    Task<CashboxDto> CreateAsync(CreateCashboxDto dto);
    Task<CashboxDto> UpdateAsync(Guid id, UpdateCashboxDto dto);
    Task DeleteAsync(Guid id);
    Task<CashboxDto?> GetByIdAsync(Guid id);
    Task<PagedResult<CashboxListDto>> SearchAsync(CashboxSearchDto dto);
}
