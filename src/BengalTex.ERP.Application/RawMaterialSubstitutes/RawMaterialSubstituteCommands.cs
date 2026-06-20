using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.RawMaterialSubstitutes;

// ── Create ──
public sealed record CreateRawMaterialSubstituteCommand(
    int RawMaterialId, int SubstituteRawMaterialId, decimal ConversionFactor, string? Notes, bool IsActive)
    : IRequest<ApiResponse<int>>;

public sealed class CreateRawMaterialSubstituteCommandValidator : AbstractValidator<CreateRawMaterialSubstituteCommand>
{
    public CreateRawMaterialSubstituteCommandValidator()
    {
        RuleFor(x => x.RawMaterialId).GreaterThan(0);
        RuleFor(x => x.SubstituteRawMaterialId).GreaterThan(0);
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x).Must(x => x.RawMaterialId != x.SubstituteRawMaterialId)
            .WithMessage("A material cannot be its own substitute.");
    }
}

internal sealed class CreateRawMaterialSubstituteCommandHandler : IRequestHandler<CreateRawMaterialSubstituteCommand, ApiResponse<int>>
{
    private readonly IRepository<RawMaterialSubstitute> _repo;
    private readonly IRepository<Domain.Entities.RawMaterial> _rmRepo;
    private readonly IUnitOfWork _uow;

    public CreateRawMaterialSubstituteCommandHandler(
        IRepository<RawMaterialSubstitute> repo, IRepository<Domain.Entities.RawMaterial> rmRepo, IUnitOfWork uow)
    { _repo = repo; _rmRepo = rmRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateRawMaterialSubstituteCommand cmd, CancellationToken ct)
    {
        if (await _rmRepo.GetByIdAsync(cmd.RawMaterialId, ct) is null) return ApiResponse<int>.Fail("Raw material not found.");
        if (await _rmRepo.GetByIdAsync(cmd.SubstituteRawMaterialId, ct) is null) return ApiResponse<int>.Fail("Substitute material not found.");

        var dup = await _repo.Query().AnyAsync(s => s.RawMaterialId == cmd.RawMaterialId && s.SubstituteRawMaterialId == cmd.SubstituteRawMaterialId, ct);
        if (dup) return ApiResponse<int>.Fail("This substitute is already listed for the material.");

        var e = new RawMaterialSubstitute
        {
            RawMaterialId = cmd.RawMaterialId,
            SubstituteRawMaterialId = cmd.SubstituteRawMaterialId,
            ConversionFactor = cmd.ConversionFactor,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim(),
            IsActive = cmd.IsActive
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Substitute added.");
    }
}

// ── Update ──
public sealed record UpdateRawMaterialSubstituteCommand(
    int Id, decimal ConversionFactor, string? Notes, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed class UpdateRawMaterialSubstituteCommandValidator : AbstractValidator<UpdateRawMaterialSubstituteCommand>
{
    public UpdateRawMaterialSubstituteCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ConversionFactor).GreaterThan(0);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}

internal sealed class UpdateRawMaterialSubstituteCommandHandler : IRequestHandler<UpdateRawMaterialSubstituteCommand, ApiResponse<int>>
{
    private readonly IRepository<RawMaterialSubstitute> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateRawMaterialSubstituteCommandHandler(IRepository<RawMaterialSubstitute> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateRawMaterialSubstituteCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<int>.Fail("Substitute not found.");
        e.ConversionFactor = cmd.ConversionFactor;
        e.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        e.IsActive = cmd.IsActive;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Substitute updated.");
    }
}

// ── Delete ──
public sealed record DeleteRawMaterialSubstituteCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteRawMaterialSubstituteCommandHandler : IRequestHandler<DeleteRawMaterialSubstituteCommand, ApiResponse>
{
    private readonly IRepository<RawMaterialSubstitute> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteRawMaterialSubstituteCommandHandler(IRepository<RawMaterialSubstitute> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteRawMaterialSubstituteCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Substitute not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Substitute removed.");
    }
}
