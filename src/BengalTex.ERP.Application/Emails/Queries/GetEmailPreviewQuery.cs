using BengalTex.ERP.Application.Emails.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Emails.Queries;

/// <summary>
/// Pre-renders a document's email body + default subject + default recipient so the UI
/// dialog can pre-fill itself. The actual send happens via <c>SendDocumentEmailCommand</c>
/// with the (possibly user-edited) subject/body/recipients.
/// </summary>
public sealed record GetEmailPreviewQuery(string SourceType, long SourceId)
    : IRequest<ApiResponse<EmailPreviewDto>>;

public sealed class GetEmailPreviewQueryValidator : AbstractValidator<GetEmailPreviewQuery>
{
    public GetEmailPreviewQueryValidator()
    {
        RuleFor(x => x.SourceType).NotEmpty()
            .Must(t => IDocumentEmailService.SupportedSourceTypes.Contains(t))
            .WithMessage("Unsupported document type for email.");
        RuleFor(x => x.SourceId).GreaterThan(0);
    }
}

internal sealed class GetEmailPreviewQueryHandler
    : IRequestHandler<GetEmailPreviewQuery, ApiResponse<EmailPreviewDto>>
{
    private readonly IDocumentEmailService _docEmail;
    public GetEmailPreviewQueryHandler(IDocumentEmailService docEmail) => _docEmail = docEmail;

    public async Task<ApiResponse<EmailPreviewDto>> Handle(GetEmailPreviewQuery q, CancellationToken ct)
    {
        var rendered = await _docEmail.RenderAsync(q.SourceType, q.SourceId, ct);
        if (rendered is null) return ApiResponse<EmailPreviewDto>.Fail("Document not found.");
        return ApiResponse<EmailPreviewDto>.Ok(new EmailPreviewDto(
            q.SourceType, q.SourceId, rendered.SourceCode,
            rendered.DefaultSubject, rendered.HtmlBody, rendered.DefaultToAddress));
    }
}
