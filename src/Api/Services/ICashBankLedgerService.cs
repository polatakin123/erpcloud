using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

public interface ICashBankLedgerService
{
    Task<PagedResult<CashBankLedgerDto>> GetLedgerAsync(CashBankLedgerSearchDto dto);
    Task<CashBankBalanceDto> GetBalanceAsync(CashBankBalanceQueryDto dto);
}
