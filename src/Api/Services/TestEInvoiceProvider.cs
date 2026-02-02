using ErpCloud.Api.Entities;
using ErpCloud.Api.Models;

namespace ErpCloud.Api.Services;

/// <summary>
/// Test provider that simulates real-world behavior with delays and random outcomes
/// </summary>
public class TestEInvoiceProvider : IEInvoiceProvider
{
    private static readonly Random _random = new();
    
    public string Code => "TEST";

    public async Task<SendResult> SendAsync(EDocument doc, string ublXml)
    {
        // Simulate network delay
        await Task.Delay(TimeSpan.FromSeconds(_random.Next(1, 3)));

        // Random outcome: 70% success, 30% failure
        var success = _random.Next(100) < 70;

        if (success)
        {
            return new SendResult(
                Success: true,
                Status: "SENT",
                Message: "Document sent successfully",
                EnvelopeId: $"ENV-{Guid.NewGuid():N}"
            );
        }
        else
        {
            return new SendResult(
                Success: false,
                Status: "ERROR",
                Message: "Simulated provider error: Network timeout"
            );
        }
    }

    public async Task<StatusResult> CheckStatusAsync(EDocument doc)
    {
        // Simulate network delay
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(500, 1500)));

        // If status is SENT, simulate GIB acceptance (80% accepted, 20% rejected)
        if (doc.Status == "SENT")
        {
            var accepted = _random.Next(100) < 80;
            
            if (accepted)
            {
                return new StatusResult(
                    Status: "ACCEPTED",
                    Message: "Document accepted by GIB",
                    GIBReference: $"GIB-{Guid.NewGuid():N}"
                );
            }
            else
            {
                return new StatusResult(
                    Status: "REJECTED",
                    Message: "Simulated rejection: Invalid tax number"
                );
            }
        }

        // If status is ERROR, keep it as rejected
        if (doc.Status == "ERROR")
        {
            return new StatusResult(
                Status: "REJECTED",
                Message: "Document in error state"
            );
        }

        // Otherwise, still pending
        return new StatusResult(
            Status: "PENDING",
            Message: "Document processing"
        );
    }

    public async Task CancelAsync(EDocument doc)
    {
        // Simulate network delay
        await Task.Delay(TimeSpan.FromMilliseconds(_random.Next(500, 1000)));

        // In test provider, cancellation always succeeds
        // Real providers might return errors
    }
}
