using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Application.Emails.Commands;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Emails the buyer a 2-PDF export bundle for a Customer Invoice:
/// Commercial Invoice + Packing List. Both are rendered fresh from the
/// invoice's current data and attached. Records a single audit row in
/// SentEmail (SourceType = "ExportBundle"), and on send-failure marks it Failed.
/// </summary>
public sealed record SendExportBundleEmailCommand(
    long InvoiceId,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string HtmlBody
) : IRequest<ApiResponse<long>>;

public sealed class SendExportBundleEmailCommandValidator : AbstractValidator<SendExportBundleEmailCommand>
{
    public SendExportBundleEmailCommandValidator()
    {
        RuleFor(x => x.InvoiceId).GreaterThan(0);
        RuleFor(x => x.ToAddresses).NotEmpty()
            .Must(t => SendDocumentEmailCommandValidator.SplitAddresses(t).Any())
            .WithMessage("Provide at least one valid recipient.");
        RuleFor(x => x.CcAddresses).MaximumLength(1000);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }
}

internal sealed class SendExportBundleEmailCommandHandler
    : IRequestHandler<SendExportBundleEmailCommand, ApiResponse<long>>
{
    private readonly IMediator _mediator;
    private readonly IRepository<SentEmail, long> _repo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;
    private readonly IExportPdfRenderer _pdf;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<SendExportBundleEmailCommandHandler> _logger;

    public SendExportBundleEmailCommandHandler(
        IMediator mediator,
        IRepository<SentEmail, long> repo,
        IRepository<Domain.Entities.Company> companyRepo,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IExportPdfRenderer pdf,
        ICurrentUserService currentUser,
        ILogger<SendExportBundleEmailCommandHandler> logger)
    {
        _mediator = mediator; _repo = repo; _companyRepo = companyRepo; _uow = uow;
        _emailSender = emailSender; _pdf = pdf; _currentUser = currentUser; _logger = logger;
    }

    public async Task<ApiResponse<long>> Handle(SendExportBundleEmailCommand cmd, CancellationToken ct)
    {
        var invRes = await _mediator.Send(new GetCustomerInvoiceByIdQuery(cmd.InvoiceId), ct);
        if (!invRes.Success || invRes.Data is null)
            return ApiResponse<long>.Fail(invRes.Message ?? "Invoice not found.");
        var inv = invRes.Data;

        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);
        var companyName = company?.Name ?? "Our Company";
        var companyAddress = company is null
            ? null
            : string.Join(", ",
                new[] { company.AddressLine1, company.AddressLine2, company.City, company.Country }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));

        var ciPdf = _pdf.RenderCommercialInvoice(inv, companyName, companyAddress);
        var plPdf = _pdf.RenderPackingList(inv, companyName, companyAddress);
        var attachments = new List<EmailAttachment>
        {
            new($"Commercial-Invoice-{inv.Code}.pdf", "application/pdf", ciPdf),
            new($"Packing-List-{inv.Code}.pdf", "application/pdf", plPdf),
        };

        var toList = SendDocumentEmailCommandValidator.SplitAddresses(cmd.ToAddresses);
        var ccList = SendDocumentEmailCommandValidator.SplitAddresses(cmd.CcAddresses);
        var allRecipients = toList.Concat(ccList).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var entity = new SentEmail
        {
            SentAt = DateTimeOffset.UtcNow,
            SentByUser = _currentUser.UserName ?? "system",
            SourceType = "ExportBundle",
            SourceId = inv.Id,
            SourceCode = inv.Code,
            ToAddresses = string.Join(", ", toList),
            CcAddresses = ccList.Count > 0 ? string.Join(", ", ccList) : null,
            Subject = cmd.Subject.Trim(),
            Body = cmd.HtmlBody,
            Status = SentEmailStatus.Sent
        };

        try
        {
            await _emailSender.SendAsync(allRecipients, cmd.Subject.Trim(), cmd.HtmlBody, attachments, ct);
        }
        catch (Exception ex)
        {
            entity.Status = SentEmailStatus.Failed;
            entity.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _logger.LogError(ex, "Export bundle email failed for invoice {Code}", inv.Code);
        }

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return entity.Status == SentEmailStatus.Sent
            ? ApiResponse<long>.Ok(entity.Id, $"Export bundle sent for {inv.Code}.")
            : ApiResponse<long>.Fail($"Send failed: {entity.ErrorMessage}");
    }
}
