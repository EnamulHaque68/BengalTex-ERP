using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Application.Emails.Commands;

/// <summary>
/// Sends an email about a specific source document. Either uses the renderer (if no custom
/// body supplied) or sends the user-supplied subject/body. Always writes an audit row to
/// <see cref="SentEmail"/> — including failures. To/Cc accept comma- or semicolon-separated
/// lists which are split + cleaned before send.
/// </summary>
public sealed record SendDocumentEmailCommand(
    string SourceType,
    long SourceId,
    string ToAddresses,            // comma or semicolon separated
    string? CcAddresses,
    string Subject,
    string HtmlBody
) : IRequest<ApiResponse<long>>;

public sealed class SendDocumentEmailCommandValidator : AbstractValidator<SendDocumentEmailCommand>
{
    public SendDocumentEmailCommandValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty()
            .Must(t => IDocumentEmailService.SupportedSourceTypes.Contains(t))
            .WithMessage("Unsupported document type for email.");
        RuleFor(x => x.SourceId).GreaterThan(0);
        RuleFor(x => x.ToAddresses).NotEmpty()
            .Must(HasAtLeastOneRecipient)
            .WithMessage("Provide at least one valid recipient email address.");
        RuleFor(x => x.CcAddresses).MaximumLength(1000);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }

    private static bool HasAtLeastOneRecipient(string list)
        => SplitAddresses(list).Any();

    internal static IReadOnlyList<string> SplitAddresses(string? list)
    {
        if (string.IsNullOrWhiteSpace(list)) return Array.Empty<string>();
        return list.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Where(x => x.Contains('@'))
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();
    }
}

internal sealed class SendDocumentEmailCommandHandler
    : IRequestHandler<SendDocumentEmailCommand, ApiResponse<long>>
{
    private readonly IRepository<SentEmail, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;
    private readonly IDocumentEmailService _docEmail;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SendDocumentEmailCommandHandler> _logger;

    public SendDocumentEmailCommandHandler(
        IRepository<SentEmail, long> repo,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IDocumentEmailService docEmail,
        ICurrentUserService currentUser,
        ILogger<SendDocumentEmailCommandHandler> logger)
    {
        _repo = repo; _uow = uow; _emailSender = emailSender;
        _docEmail = docEmail; _currentUser = currentUser; _logger = logger;
    }

    public async Task<ApiResponse<long>> Handle(SendDocumentEmailCommand cmd, CancellationToken ct)
    {
        // Resolve sourceCode (best-effort — the renderer also returns it)
        var rendered = await _docEmail.RenderAsync(cmd.SourceType, cmd.SourceId, ct);
        if (rendered is null) return ApiResponse<long>.Fail("Document not found.");

        var toList = SendDocumentEmailCommandValidator.SplitAddresses(cmd.ToAddresses);
        var ccList = SendDocumentEmailCommandValidator.SplitAddresses(cmd.CcAddresses);
        var allRecipients = toList.Concat(ccList).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var entity = new SentEmail
        {
            SentAt = DateTimeOffset.UtcNow,
            SentByUser = _currentUser.UserName ?? "system",
            SourceType = cmd.SourceType,
            SourceId = cmd.SourceId,
            SourceCode = rendered.SourceCode,
            ToAddresses = string.Join(", ", toList),
            CcAddresses = ccList.Count > 0 ? string.Join(", ", ccList) : null,
            Subject = cmd.Subject.Trim(),
            Body = cmd.HtmlBody,
            Status = SentEmailStatus.Sent
        };

        try
        {
            await _emailSender.SendAsync(allRecipients, cmd.Subject.Trim(), cmd.HtmlBody, ct);
        }
        catch (Exception ex)
        {
            entity.Status = SentEmailStatus.Failed;
            entity.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _logger.LogError(ex, "Email send failed for {Source} {Code}", cmd.SourceType, rendered.SourceCode);
        }

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return entity.Status == SentEmailStatus.Sent
            ? ApiResponse<long>.Ok(entity.Id, $"Email sent for {rendered.SourceCode}.")
            : ApiResponse<long>.Fail($"Send failed: {entity.ErrorMessage}");
    }
}
