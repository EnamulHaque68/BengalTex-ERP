using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Shared.Common;

namespace BengalTex.ERP.Application.Services;

/// <summary>
/// Records + dispatches notifications. Email/SMS go via the configured gateway (logged
/// in dev); InApp is record-only. Lives behind this interface (impl in Infrastructure)
/// because <c>Notification</c> is an Infrastructure cross-cutting entity.
/// <see cref="NotifyAsync"/> mutates the tracked context but does NOT SaveChanges — the
/// caller commits (so a notification raised inside another command is atomic with it).
/// </summary>
public interface INotificationService
{
    Task NotifyAsync(
        string channel, string recipient, string subject, string body,
        string? relatedType = null, long? relatedId = null, CancellationToken ct = default);

    Task<PagedResult<NotificationDto>> QueryAsync(
        PagedQueryParameters parameters, string? channel, string? status, CancellationToken ct = default);
}

public static class NotificationChannels
{
    public const string Email = "Email";
    public const string Sms = "Sms";
    public const string InApp = "InApp";
}

public sealed record NotificationDto(
    long Id,
    string Channel,
    string Recipient,
    string Subject,
    string Body,
    string? RelatedEntityType,
    long? RelatedEntityId,
    string Status,
    string? Error,
    DateTimeOffset? SentAt,
    DateTimeOffset CreatedAt);
