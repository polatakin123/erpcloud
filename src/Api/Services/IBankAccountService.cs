using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

public interface IBankAccountService
{
    Task<BankAccountDto> CreateAsync(CreateBankAccountDto dto);
    Task<BankAccountDto> UpdateAsync(Guid id, UpdateBankAccountDto dto);
    Task DeleteAsync(Guid id);
    Task<BankAccountDto?> GetByIdAsync(Guid id);
    Task<PagedResult<BankAccountListDto>> SearchAsync(BankAccountSearchDto dto);
}
