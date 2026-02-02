using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

/// <summary>
/// Provider interface for e-Invoice/e-Archive integration
/// </summary>
public interface IEInvoiceProvider
{
    string Code { get; }
    
    Task<SendResult> SendAsync(EDocument doc, string ublXml);
    
    Task<StatusResult> CheckStatusAsync(EDocument doc);
    
    Task CancelAsync(EDocument doc);
}
