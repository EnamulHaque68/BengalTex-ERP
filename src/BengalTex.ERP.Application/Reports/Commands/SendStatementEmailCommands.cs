using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Emails.Commands;
using BengalTex.ERP.Application.Reports.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BengalTex.ERP.Application.Reports.Commands;

/// <summary>
/// Emails the customer their Statement of Account for the given window as an attached
/// PDF (rendered fresh via <see cref="IStatementPdfRenderer"/>). Audit row in SentEmail
/// with SourceType = "CustomerStatement" (SourceId = CustomerId). AP mirror below.
/// </summary>
public sealed record SendCustomerStatementEmailCommand(
    int CustomerId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string HtmlBody
) : IRequest<ApiResponse<long>>;

public sealed record SendSupplierStatementEmailCommand(
    int SupplierId,
    DateOnly? FromDate,
    DateOnly? ToDate,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string HtmlBody
) : IRequest<ApiResponse<long>>;

public sealed class SendCustomerStatementEmailCommandValidator : AbstractValidator<SendCustomerStatementEmailCommand>
{
    public SendCustomerStatementEmailCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.ToAddresses).NotEmpty()
            .Must(t => SendDocumentEmailCommandValidator.SplitAddresses(t).Any())
            .WithMessage("Provide at least one valid recipient.");
        RuleFor(x => x.CcAddresses).MaximumLength(1000);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }
}

public sealed class SendSupplierStatementEmailCommandValidator : AbstractValidator<SendSupplierStatementEmailCommand>
{
    public SendSupplierStatementEmailCommandValidator()
    {
        RuleFor(x => x.SupplierId).GreaterThan(0);
        RuleFor(x => x.ToAddresses).NotEmpty()
            .Must(t => SendDocumentEmailCommandValidator.SplitAddresses(t).Any())
            .WithMessage("Provide at least one valid recipient.");
        RuleFor(x => x.CcAddresses).MaximumLength(1000);
        RuleFor(x => x.Subject).NotEmpty().MaximumLength(300);
        RuleFor(x => x.HtmlBody).NotEmpty();
    }
}

/// <summary>Shared plumbing for both statement-email handlers.</summary>
internal sealed class StatementEmailDispatcher
{
    private readonly IRepository<SentEmail, long> _sentRepo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;
    private readonly IUnitOfWork _uow;
    private readonly IEmailSender _emailSender;
    private readonly IFileStorage _files;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<StatementEmailDispatcher> _logger;

    public StatementEmailDispatcher(
        IRepository<SentEmail, long> sentRepo,
        IRepository<Domain.Entities.Company> companyRepo,
        IUnitOfWork uow,
        IEmailSender emailSender,
        IFileStorage files,
        ICurrentUserService currentUser,
        ILogger<StatementEmailDispatcher> logger)
    {
        _sentRepo = sentRepo; _companyRepo = companyRepo; _uow = uow;
        _emailSender = emailSender; _files = files; _currentUser = currentUser; _logger = logger;
    }

    public async Task<(string CompanyName, string? CompanyAddress, byte[]? CompanyLogo)> GetCompanyAsync(CancellationToken ct)
    {
        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);
        if (company is null) return ("Our Company", null, null);
        var address = string.Join(", ",
            new[] { company.AddressLine1, company.AddressLine2, company.City, company.Country }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        var logo = await Company.CompanyLogoLoader.LoadAsync(company.LogoUrl, _files, ct);
        return (company.Name, address, logo);
    }

    public async Task<ApiResponse<long>> SendAsync(
        string sourceType, long sourceId, string sourceCode,
        string toAddresses, string? ccAddresses, string subject, string htmlBody,
        EmailAttachment attachment, CancellationToken ct)
    {
        var toList = SendDocumentEmailCommandValidator.SplitAddresses(toAddresses);
        var ccList = SendDocumentEmailCommandValidator.SplitAddresses(ccAddresses);
        var allRecipients = toList.Concat(ccList).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        var entity = new SentEmail
        {
            SentAt = DateTimeOffset.UtcNow,
            SentByUser = _currentUser.UserName ?? "system",
            SourceType = sourceType,
            SourceId = sourceId,
            SourceCode = sourceCode,
            ToAddresses = string.Join(", ", toList),
            CcAddresses = ccList.Count > 0 ? string.Join(", ", ccList) : null,
            Subject = subject.Trim(),
            Body = htmlBody,
            Status = SentEmailStatus.Sent
        };

        try
        {
            await _emailSender.SendAsync(allRecipients, subject.Trim(), htmlBody, new[] { attachment }, ct);
        }
        catch (Exception ex)
        {
            entity.Status = SentEmailStatus.Failed;
            entity.ErrorMessage = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
            _logger.LogError(ex, "Statement email failed for {SourceType} {Code}", sourceType, sourceCode);
        }

        await _sentRepo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        return entity.Status == SentEmailStatus.Sent
            ? ApiResponse<long>.Ok(entity.Id, $"Statement sent for {sourceCode}.")
            : ApiResponse<long>.Fail($"Send failed: {entity.ErrorMessage}");
    }
}

internal sealed class SendCustomerStatementEmailCommandHandler
    : IRequestHandler<SendCustomerStatementEmailCommand, ApiResponse<long>>
{
    private readonly IMediator _mediator;
    private readonly IStatementPdfRenderer _pdf;
    private readonly StatementEmailDispatcher _dispatcher;

    public SendCustomerStatementEmailCommandHandler(
        IMediator mediator, IStatementPdfRenderer pdf, StatementEmailDispatcher dispatcher)
    {
        _mediator = mediator; _pdf = pdf; _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<long>> Handle(SendCustomerStatementEmailCommand cmd, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetCustomerStatementQuery(cmd.CustomerId, cmd.FromDate, cmd.ToDate), ct);
        if (!res.Success || res.Data is null)
            return ApiResponse<long>.Fail(res.Message ?? "Statement could not be generated.");
        var report = res.Data;

        var (companyName, companyAddress, companyLogo) = await _dispatcher.GetCompanyAsync(ct);
        var pdfBytes = _pdf.RenderCustomerStatement(report, companyName, companyAddress, companyLogo);
        var attachment = new EmailAttachment(
            $"Statement-{report.CustomerCode}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf",
            "application/pdf", pdfBytes);

        return await _dispatcher.SendAsync(
            "CustomerStatement", report.CustomerId, report.CustomerCode,
            cmd.ToAddresses, cmd.CcAddresses, cmd.Subject, cmd.HtmlBody, attachment, ct);
    }
}

internal sealed class SendSupplierStatementEmailCommandHandler
    : IRequestHandler<SendSupplierStatementEmailCommand, ApiResponse<long>>
{
    private readonly IMediator _mediator;
    private readonly IStatementPdfRenderer _pdf;
    private readonly StatementEmailDispatcher _dispatcher;

    public SendSupplierStatementEmailCommandHandler(
        IMediator mediator, IStatementPdfRenderer pdf, StatementEmailDispatcher dispatcher)
    {
        _mediator = mediator; _pdf = pdf; _dispatcher = dispatcher;
    }

    public async Task<ApiResponse<long>> Handle(SendSupplierStatementEmailCommand cmd, CancellationToken ct)
    {
        var res = await _mediator.Send(new GetSupplierStatementQuery(cmd.SupplierId, cmd.FromDate, cmd.ToDate), ct);
        if (!res.Success || res.Data is null)
            return ApiResponse<long>.Fail(res.Message ?? "Statement could not be generated.");
        var report = res.Data;

        var (companyName, companyAddress, companyLogo) = await _dispatcher.GetCompanyAsync(ct);
        var pdfBytes = _pdf.RenderSupplierStatement(report, companyName, companyAddress, companyLogo);
        var attachment = new EmailAttachment(
            $"Supplier-Statement-{report.SupplierCode}-{report.FromDate:yyyyMMdd}-{report.ToDate:yyyyMMdd}.pdf",
            "application/pdf", pdfBytes);

        return await _dispatcher.SendAsync(
            "SupplierStatement", report.SupplierId, report.SupplierCode,
            cmd.ToAddresses, cmd.CcAddresses, cmd.Subject, cmd.HtmlBody, attachment, ct);
    }
}
