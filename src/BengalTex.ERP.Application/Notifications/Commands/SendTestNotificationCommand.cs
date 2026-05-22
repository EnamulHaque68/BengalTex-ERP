using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Notifications.Commands;

/// <summary>
/// Sends a one-off notification on the chosen channel (Email/Sms/InApp). Useful to verify
/// the configured Email/SMS gateway. The attempt is recorded with its outcome and committed.
/// </summary>
public sealed record SendTestNotificationCommand(
    string Channel,
    string Recipient,
    string Subject,
    string Body
) : IRequest<ApiResponse<bool>>;

internal sealed class SendTestNotificationCommandHandler
    : IRequestHandler<SendTestNotificationCommand, ApiResponse<bool>>
{
    private readonly INotificationService _notifications;
    private readonly IUnitOfWork _uow;

    public SendTestNotificationCommandHandler(INotificationService notifications, IUnitOfWork uow)
    {
        _notifications = notifications;
        _uow = uow;
    }

    public async Task<ApiResponse<bool>> Handle(
        SendTestNotificationCommand request, CancellationToken cancellationToken)
    {
        await _notifications.NotifyAsync(
            request.Channel, request.Recipient, request.Subject, request.Body,
            relatedType: null, relatedId: null, cancellationToken);

        await _uow.SaveChangesAsync(cancellationToken);
        return ApiResponse<bool>.Ok(true, "Notification dispatched (see the log for the outcome).");
    }
}

public sealed class SendTestNotificationCommandValidator : AbstractValidator<SendTestNotificationCommand>
{
    private static readonly string[] AllowedChannels =
        { NotificationChannels.Email, NotificationChannels.Sms, NotificationChannels.InApp };

    public SendTestNotificationCommandValidator()
    {
        RuleFor(x => x.Channel)
            .NotEmpty()
            .Must(c => AllowedChannels.Contains(c))
            .WithMessage("Channel must be Email, Sms or InApp.");
        RuleFor(x => x.Recipient).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Body).NotEmpty();
    }
}
