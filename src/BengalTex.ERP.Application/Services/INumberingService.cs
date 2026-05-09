namespace BengalTex.ERP.Application.Services;

public interface INumberingService
{
    /// <summary>
    /// Generates the next document number for the given series code.
    /// Concurrency-safe via row lock.
    /// </summary>
    Task<string> NextAsync(string seriesCode, int? factoryId = null, CancellationToken ct = default);
}