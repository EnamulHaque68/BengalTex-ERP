using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.JobCards.Commands;

/// <summary>
/// Operator action against a job card — drives the state machine and writes a JobCardScan row.
/// Accepts either the JobCard Id or the JobCard Code (so a QR-decoded string works directly).
///
/// State transitions:
///   Open       → Start              → InProgress (StartedAt + LastResumedAt set)
///   InProgress → Pause              → OnHold     (ActiveMinutes accumulates)
///   OnHold     → Resume             → InProgress (LastResumedAt set)
///   In*/OnHold → Complete           → Completed  (ActiveMinutes finalised; qty/rejected captured)
///   Open/InPr  → Cancel             → Cancelled
///   any        → QcCheck            → no state change (audit log only)
/// </summary>
public sealed record ScanJobCardCommand(
    long? JobCardId,
    string? Code,
    string ScanType,        // "Start" | "Pause" | "Resume" | "Complete" | "QcCheck" | "Cancel"
    decimal? Quantity,
    decimal? RejectedQuantity,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class ScanJobCardCommandValidator : AbstractValidator<ScanJobCardCommand>
{
    public ScanJobCardCommandValidator()
    {
        RuleFor(x => x.ScanType).NotEmpty()
            .Must(s => Enum.TryParse<JobCardScanType>(s, out _))
            .WithMessage("ScanType must be one of Start, Pause, Resume, Complete, QcCheck, Cancel.");
        RuleFor(x => x).Must(x => (x.JobCardId.HasValue && x.JobCardId > 0) || !string.IsNullOrWhiteSpace(x.Code))
            .WithMessage("Provide either JobCardId or Code.");
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0).When(x => x.Quantity.HasValue);
        RuleFor(x => x.RejectedQuantity).GreaterThanOrEqualTo(0).When(x => x.RejectedQuantity.HasValue);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class ScanJobCardCommandHandler : IRequestHandler<ScanJobCardCommand, ApiResponse<long>>
{
    private readonly IRepository<JobCard, long> _repo;
    private readonly IRepository<JobCardScan, long> _scanRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public ScanJobCardCommandHandler(
        IRepository<JobCard, long> repo,
        IRepository<JobCardScan, long> scanRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _repo = repo; _scanRepo = scanRepo; _uow = uow;
        _currentUser = currentUser; _clock = clock;
    }

    public async Task<ApiResponse<long>> Handle(ScanJobCardCommand cmd, CancellationToken ct)
    {
        var jc = cmd.JobCardId.HasValue
            ? await _repo.GetByIdAsync(cmd.JobCardId.Value, ct)
            : await _repo.Query().FirstOrDefaultAsync(j => j.Code == cmd.Code!.Trim().ToUpper(), ct);
        if (jc is null) return ApiResponse<long>.Fail("Job card not found.");

        var scanType = Enum.Parse<JobCardScanType>(cmd.ScanType);
        var now = _clock.UtcNow;

        // State machine
        switch (scanType)
        {
            case JobCardScanType.Start:
                if (jc.Status != JobCardStatus.Open)
                    return ApiResponse<long>.Fail($"Cannot Start a job card that is {jc.Status}.");
                jc.Status = JobCardStatus.InProgress;
                jc.StartedAt = now;
                jc.LastResumedAt = now;
                break;

            case JobCardScanType.Pause:
                if (jc.Status != JobCardStatus.InProgress)
                    return ApiResponse<long>.Fail($"Cannot Pause a job card that is {jc.Status}.");
                jc.ActiveMinutes = (jc.ActiveMinutes ?? 0) + ElapsedMinutes(jc.LastResumedAt, now);
                jc.LastResumedAt = null;
                jc.Status = JobCardStatus.OnHold;
                break;

            case JobCardScanType.Resume:
                if (jc.Status != JobCardStatus.OnHold)
                    return ApiResponse<long>.Fail($"Cannot Resume a job card that is {jc.Status}.");
                jc.Status = JobCardStatus.InProgress;
                jc.LastResumedAt = now;
                break;

            case JobCardScanType.Complete:
                if (jc.Status != JobCardStatus.InProgress && jc.Status != JobCardStatus.OnHold)
                    return ApiResponse<long>.Fail($"Cannot Complete a job card that is {jc.Status}.");
                if (jc.Status == JobCardStatus.InProgress)
                {
                    jc.ActiveMinutes = (jc.ActiveMinutes ?? 0) + ElapsedMinutes(jc.LastResumedAt, now);
                    jc.LastResumedAt = null;
                }
                jc.Status = JobCardStatus.Completed;
                jc.CompletedAt = now;
                jc.CompletedBy = _currentUser.UserName ?? _currentUser.UserId;
                jc.CompletedQuantity = cmd.Quantity ?? jc.Quantity;
                jc.RejectedQuantity = cmd.RejectedQuantity ?? jc.RejectedQuantity;
                break;

            case JobCardScanType.Cancel:
                if (jc.Status == JobCardStatus.Completed)
                    return ApiResponse<long>.Fail("Cannot cancel a completed job card.");
                if (jc.Status == JobCardStatus.InProgress)
                {
                    jc.ActiveMinutes = (jc.ActiveMinutes ?? 0) + ElapsedMinutes(jc.LastResumedAt, now);
                    jc.LastResumedAt = null;
                }
                jc.Status = JobCardStatus.Cancelled;
                break;

            case JobCardScanType.QcCheck:
                // no state change — audit only
                break;
        }

        _repo.Update(jc);
        await _scanRepo.AddAsync(new JobCardScan
        {
            JobCardId = jc.Id,
            ScanType = scanType,
            ScannedAt = now,
            ScannedBy = _currentUser.UserName ?? _currentUser.UserId,
            Quantity = cmd.Quantity,
            RejectedQuantity = cmd.RejectedQuantity,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        }, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(jc.Id, $"Job card {jc.Code} → {jc.Status}.");
    }

    private static int ElapsedMinutes(DateTimeOffset? from, DateTimeOffset to)
    {
        if (!from.HasValue) return 0;
        var minutes = (int)(to - from.Value).TotalMinutes;
        return Math.Max(0, minutes);
    }
}
