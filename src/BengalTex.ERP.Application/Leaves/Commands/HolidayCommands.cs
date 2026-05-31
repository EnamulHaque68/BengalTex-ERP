using BengalTex.ERP.Application.Leaves.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Leaves.Commands;

// ── List ──
public sealed record GetHolidaysQuery(int? Year = null, bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<HolidayDto>>>;

internal sealed class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, ApiResponse<IReadOnlyList<HolidayDto>>>
{
    private readonly IRepository<Holiday> _repo;
    public GetHolidaysQueryHandler(IRepository<Holiday> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<HolidayDto>>> Handle(GetHolidaysQuery request, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!request.IncludeInactive) q = q.Where(h => h.IsActive);
        if (request.Year.HasValue)
        {
            var year = request.Year.Value;
            q = q.Where(h => h.Date.Year == year);
        }
        var items = await q.OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Date, h.Name, h.Description, h.IsActive))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<HolidayDto>>.Ok(items);
    }
}

// ── Create ──
public sealed record CreateHolidayCommand(DateOnly Date, string Name, string? Description) : IRequest<ApiResponse<int>>;

public sealed class CreateHolidayCommandValidator : AbstractValidator<CreateHolidayCommand>
{
    public CreateHolidayCommandValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, ApiResponse<int>>
{
    private readonly IRepository<Holiday> _repo;
    private readonly IUnitOfWork _uow;
    public CreateHolidayCommandHandler(IRepository<Holiday> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateHolidayCommand cmd, CancellationToken ct)
    {
        var name = cmd.Name.Trim();
        if (await _repo.Query().AnyAsync(h => h.Date == cmd.Date && h.Name == name, ct))
            return ApiResponse<int>.Fail($"Holiday '{name}' on {cmd.Date:yyyy-MM-dd} already exists.");
        var e = new Holiday
        {
            Date = cmd.Date, Name = name,
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            IsActive = true
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Holiday created.");
    }
}

// ── Update ──
public sealed record UpdateHolidayCommand(int Id, DateOnly Date, string Name, string? Description, bool IsActive) : IRequest<ApiResponse<int>>;

public sealed class UpdateHolidayCommandValidator : AbstractValidator<UpdateHolidayCommand>
{
    public UpdateHolidayCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

internal sealed class UpdateHolidayCommandHandler : IRequestHandler<UpdateHolidayCommand, ApiResponse<int>>
{
    private readonly IRepository<Holiday> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateHolidayCommandHandler(IRepository<Holiday> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateHolidayCommand cmd, CancellationToken ct)
    {
        var h = await _repo.GetByIdAsync(cmd.Id, ct);
        if (h is null) return ApiResponse<int>.Fail("Holiday not found.");
        h.Date = cmd.Date; h.Name = cmd.Name.Trim();
        h.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        h.IsActive = cmd.IsActive;
        _repo.Update(h);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(h.Id, "Holiday updated.");
    }
}

// ── Delete ──
public sealed record DeleteHolidayCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand, ApiResponse>
{
    private readonly IRepository<Holiday> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteHolidayCommandHandler(IRepository<Holiday> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteHolidayCommand cmd, CancellationToken ct)
    {
        var h = await _repo.GetByIdAsync(cmd.Id, ct);
        if (h is null) return ApiResponse.Fail("Holiday not found.");
        _repo.Remove(h);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Holiday deleted.");
    }
}
