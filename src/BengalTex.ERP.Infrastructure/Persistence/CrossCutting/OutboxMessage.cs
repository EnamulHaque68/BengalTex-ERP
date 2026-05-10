using BengalTex.ERP.Domain.Common;

namespace BengalTex.ERP.Infrastructure.Persistence.CrossCutting;

/// <summary>
/// Outbox pattern. Messages written in the same transaction as the business state
/// change, then published asynchronously by a Hangfire job.
/// Use cases: cross-module side effects (e.g., auto journal entry, email/SMS dispatch).
/// </summary>
public class OutboxMessage : BaseTransactionalEntity
{
    public string Type { get; set; } = string.Empty;          // Fully qualified .NET type name
    public string Payload { get; set; } = string.Empty;       // JSON serialized event
    public DateTimeOffset OccurredOn { get; set; }
    public DateTimeOffset? ProcessedOn { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}