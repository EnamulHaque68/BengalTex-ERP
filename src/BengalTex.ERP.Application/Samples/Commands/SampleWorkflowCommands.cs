using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Samples.Dtos;
using BengalTex.ERP.Application.Samples.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Samples.Commands;

// ─── Start development (Requested → InDevelopment) ───────────────────────────
public sealed record StartSampleDevelopmentCommand(long Id) : IRequest<ApiResponse<SampleDto>>;

internal sealed class StartSampleDevelopmentCommandHandler : IRequestHandler<StartSampleDevelopmentCommand, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public StartSampleDevelopmentCommandHandler(IRepository<Domain.Entities.Sample, long> repo, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<SampleDto>> Handle(StartSampleDevelopmentCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse<SampleDto>.Fail("Sample not found.");
        if (s.Status != SampleStatus.Requested) return ApiResponse<SampleDto>.Fail("Only a requested sample can move into development.");
        s.Status = SampleStatus.InDevelopment;
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSampleByIdQuery(s.Id), ct);
    }
}

// ─── Submit (InDevelopment → Submitted) ──────────────────────────────────────
public sealed record SubmitSampleCommand(long Id) : IRequest<ApiResponse<SampleDto>>;

internal sealed class SubmitSampleCommandHandler : IRequestHandler<SubmitSampleCommand, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;
    public SubmitSampleCommandHandler(IRepository<Domain.Entities.Sample, long> repo, IUnitOfWork uow, IMediator mediator)
    { _repo = repo; _uow = uow; _mediator = mediator; }

    public async Task<ApiResponse<SampleDto>> Handle(SubmitSampleCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse<SampleDto>.Fail("Sample not found.");
        if (s.Status != SampleStatus.InDevelopment) return ApiResponse<SampleDto>.Fail("Only a sample in development can be submitted.");
        s.Status = SampleStatus.Submitted;
        s.SubmittedAt = DateTimeOffset.UtcNow;
        s.SubmittedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSampleByIdQuery(s.Id), ct);
    }
}

// ─── Decide (Submitted → Approved | Rejected) with buyer feedback ────────────
public sealed record DecideSampleCommand(long Id, bool Approve, string? Feedback) : IRequest<ApiResponse<SampleDto>>;

public sealed class DecideSampleCommandValidator : AbstractValidator<DecideSampleCommand>
{
    public DecideSampleCommandValidator() => RuleFor(x => x.Feedback).MaximumLength(2000);
}

internal sealed class DecideSampleCommandHandler : IRequestHandler<DecideSampleCommand, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    public DecideSampleCommandHandler(IRepository<Domain.Entities.Sample, long> repo, IUnitOfWork uow, ICurrentUserService currentUser, IMediator mediator)
    { _repo = repo; _uow = uow; _currentUser = currentUser; _mediator = mediator; }

    public async Task<ApiResponse<SampleDto>> Handle(DecideSampleCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse<SampleDto>.Fail("Sample not found.");
        if (s.Status != SampleStatus.Submitted) return ApiResponse<SampleDto>.Fail("Only a submitted sample can be approved or rejected.");
        s.Status = cmd.Approve ? SampleStatus.Approved : SampleStatus.Rejected;
        s.DecidedAt = DateTimeOffset.UtcNow;
        s.DecidedBy = _currentUser.UserName;
        s.Feedback = string.IsNullOrWhiteSpace(cmd.Feedback) ? s.Feedback : cmd.Feedback.Trim();
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSampleByIdQuery(s.Id), ct);
    }
}

// ─── Delete (before a decision) ──────────────────────────────────────────────
public sealed record DeleteSampleCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteSampleCommandHandler : IRequestHandler<DeleteSampleCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteSampleCommandHandler(IRepository<Domain.Entities.Sample, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteSampleCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse.Fail("Sample not found.");
        if (s.Status is SampleStatus.Approved or SampleStatus.Rejected)
            return ApiResponse.Fail("A decided sample cannot be deleted.");
        _repo.Remove(s);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Sample deleted.");
    }
}
