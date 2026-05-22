using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Infrastructure.Persistence;
using BengalTex.ERP.Infrastructure.Persistence.CrossCutting;
using BengalTex.ERP.Shared.Common;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Infrastructure.Services;

/// <summary>
/// Implements <see cref="INotificationService"/> over the Notifications table + the
/// Email/SMS gateways. Records every attempt with its outcome (Sent/Failed). NotifyAsync
/// does NOT save — the calling command's SaveChanges commits the notification atomically.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _db;
    private readonly IEmailSender _email;
    private readonly ISmsSender _sms;
    private readonly IDateTimeProvider _clock;

    public NotificationService(ApplicationDbContext db, IEmailSender email, ISmsSender sms, IDateTimeProvider clock)
    {
        _db = db;
        _email = email;
        _sms = sms;
        _clock = clock;
    }

    public async Task NotifyAsync(
        string channel, string recipient, string subject, string body,
        string? relatedType = null, long? relatedId = null, CancellationToken ct = default)
    {
        var n = new Notification
        {
            Channel = channel,
            Recipient = recipient,
            Subject = subject,
            Body = body,
            RelatedEntityType = relatedType,
            RelatedEntityId = relatedId,
            Status = "Pending"
        };

        try
        {
            if (channel == NotificationChannels.Email)
                await _email.SendAsync(recipient, subject, body, ct);
            else if (channel == NotificationChannels.Sms)
                await _sms.SendAsync(recipient, body, ct);
            // InApp: record-only, no external dispatch.

            n.Status = "Sent";
            n.SentAt = _clock.UtcNow;
        }
        catch (Exception ex)
        {
            n.Status = "Failed";
            n.Error = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
        }

        _db.Notifications.Add(n);   // caller owns SaveChanges
    }

    public async Task<PagedResult<NotificationDto>> QueryAsync(
        PagedQueryParameters parameters, string? channel, string? status, CancellationToken ct = default)
    {
        var query = _db.Notifications.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(channel)) query = query.Where(n => n.Channel == channel);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(n => n.Status == status);

        var search = parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(n => n.Recipient.Contains(search) || n.Subject.Contains(search));

        query = query.OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(n => new NotificationDto(
                n.Id, n.Channel, n.Recipient, n.Subject, n.Body,
                n.RelatedEntityType, n.RelatedEntityId, n.Status, n.Error, n.SentAt, n.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<NotificationDto>.Create(items, parameters.Page, parameters.PageSize, totalCount);
    }
}
