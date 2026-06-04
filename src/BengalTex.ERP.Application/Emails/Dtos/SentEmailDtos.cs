namespace BengalTex.ERP.Application.Emails.Dtos;

public sealed record SentEmailDto(
    long Id,
    DateTimeOffset SentAt,
    string SentByUser,
    string? SourceType,
    long? SourceId,
    string? SourceCode,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string Status,
    string? ErrorMessage);

/// <summary>Returned by Preview so the dialog can pre-fill subject + body + default recipient.</summary>
public sealed record EmailPreviewDto(
    string SourceType,
    long SourceId,
    string SourceCode,
    string DefaultSubject,
    string HtmlBody,
    string? DefaultToAddress);
