using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Models;
using BengalTex.ERP.Application.GatePasses.Dtos;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.GatePasses.Commands;

// ───────────────────────────────────────────────────────────────────────────
//   Helpers
// ───────────────────────────────────────────────────────────────────────────
internal static class GatePassMapping
{
    public static GatePassDto ToDto(Domain.Entities.GatePass g, DateOnly today)
    {
        var isOverdue = g.Type == GatePassType.ReturnableOut
                        && g.Status == GatePassStatus.Open
                        && g.ExpectedReturnDate.HasValue
                        && today > g.ExpectedReturnDate.Value;
        return new GatePassDto(
            g.Id, g.Code, g.PassDate, g.PassTime,
            g.Type.ToString(), g.Direction.ToString(),
            g.VehicleNumber, g.DriverName, g.DriverPhone, g.DriverNidNumber, g.TransporterName,
            g.VisitorName, g.VisitorPhone, g.VisitorOrganization, g.VisitorPurpose,
            g.ItemDescription, g.Quantity, g.FromLocation, g.ToLocation,
            g.SourceType, g.SourceId, g.SourceCode,
            g.IssuedByUser, g.ApprovedByUser,
            g.ExpectedReturnDate, g.ReturnedAt, g.ReturnedByUser, g.ReturnNotes,
            g.ClosedAt, g.Status.ToString(), isOverdue, g.Notes);
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   List
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetGatePassesQuery(
    PagedQueryParameters Parameters,
    string? Status = null,
    string? Type = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null
) : IRequest<ApiResponse<PagedResult<GatePassDto>>>;

internal sealed class GetGatePassesQueryHandler
    : IRequestHandler<GetGatePassesQuery, ApiResponse<PagedResult<GatePassDto>>>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    public GetGatePassesQueryHandler(IRepository<Domain.Entities.GatePass, long> repo) => _repo = repo;

    public async Task<ApiResponse<PagedResult<GatePassDto>>> Handle(
        GetGatePassesQuery req, CancellationToken ct)
    {
        var q = _repo.Query();
        if (!string.IsNullOrEmpty(req.Status) && Enum.TryParse<GatePassStatus>(req.Status, out var s))
            q = q.Where(x => x.Status == s);
        if (!string.IsNullOrEmpty(req.Type) && Enum.TryParse<GatePassType>(req.Type, out var t))
            q = q.Where(x => x.Type == t);
        if (req.FromDate.HasValue) q = q.Where(x => x.PassDate >= req.FromDate.Value);
        if (req.ToDate.HasValue) q = q.Where(x => x.PassDate <= req.ToDate.Value);

        var search = req.Parameters.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
            q = q.Where(x => x.Code.Contains(search)
                          || (x.VehicleNumber != null && x.VehicleNumber.Contains(search))
                          || (x.DriverName != null && x.DriverName.Contains(search))
                          || (x.VisitorName != null && x.VisitorName.Contains(search))
                          || (x.ItemDescription != null && x.ItemDescription.Contains(search)));

        q = q.OrderByDescending(x => x.PassDate).ThenByDescending(x => x.CreatedAt);

        var total = await q.CountAsync(ct);
        var entities = await q
            .Skip((req.Parameters.Page - 1) * req.Parameters.PageSize)
            .Take(req.Parameters.PageSize)
            .ToListAsync(ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        var items = entities.Select(g => GatePassMapping.ToDto(g, today)).ToList();
        return ApiResponse<PagedResult<GatePassDto>>.Ok(
            PagedResult<GatePassDto>.Create(items, req.Parameters.Page, req.Parameters.PageSize, total));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Get By Id
// ───────────────────────────────────────────────────────────────────────────
public sealed record GetGatePassByIdQuery(long Id) : IRequest<ApiResponse<GatePassDto>>;

internal sealed class GetGatePassByIdQueryHandler
    : IRequestHandler<GetGatePassByIdQuery, ApiResponse<GatePassDto>>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    public GetGatePassByIdQueryHandler(IRepository<Domain.Entities.GatePass, long> repo) => _repo = repo;

    public async Task<ApiResponse<GatePassDto>> Handle(GetGatePassByIdQuery q, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(q.Id, ct);
        if (g is null) return ApiResponse<GatePassDto>.Fail("Gate pass not found.");
        return ApiResponse<GatePassDto>.Ok(GatePassMapping.ToDto(g, DateOnly.FromDateTime(DateTime.UtcNow.Date)));
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Create (Open state)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CreateGatePassCommand(
    DateOnly PassDate,
    TimeOnly? PassTime,
    string Type,                    // GatePassType
    string Direction,               // GatePassDirection
    string? VehicleNumber,
    string? DriverName,
    string? DriverPhone,
    string? DriverNidNumber,
    string? TransporterName,
    string? VisitorName,
    string? VisitorPhone,
    string? VisitorOrganization,
    string? VisitorPurpose,
    string? ItemDescription,
    string? Quantity,
    string? FromLocation,
    string? ToLocation,
    string? SourceType,
    long? SourceId,
    string? SourceCode,
    string? ApprovedByUser,
    DateOnly? ExpectedReturnDate,
    string? Notes
) : IRequest<ApiResponse<long>>;

public sealed class CreateGatePassCommandValidator : AbstractValidator<CreateGatePassCommand>
{
    public CreateGatePassCommandValidator()
    {
        RuleFor(x => x.PassDate).NotEmpty();
        RuleFor(x => x.Type).NotEmpty()
            .Must(v => Enum.TryParse<GatePassType>(v, out _))
            .WithMessage("Invalid gate-pass type.");
        RuleFor(x => x.Direction).NotEmpty()
            .Must(v => Enum.TryParse<GatePassDirection>(v, out _))
            .WithMessage("Direction must be 'In' or 'Out'.");
        RuleFor(x => x.VehicleNumber).MaximumLength(30);
        RuleFor(x => x.DriverName).MaximumLength(100);
        RuleFor(x => x.DriverPhone).MaximumLength(30);
        RuleFor(x => x.DriverNidNumber).MaximumLength(30);
        RuleFor(x => x.TransporterName).MaximumLength(150);
        RuleFor(x => x.VisitorName).MaximumLength(100);
        RuleFor(x => x.VisitorPhone).MaximumLength(30);
        RuleFor(x => x.VisitorOrganization).MaximumLength(150);
        RuleFor(x => x.VisitorPurpose).MaximumLength(500);
        RuleFor(x => x.ItemDescription).MaximumLength(1000);
        RuleFor(x => x.Quantity).MaximumLength(100);
        RuleFor(x => x.FromLocation).MaximumLength(150);
        RuleFor(x => x.ToLocation).MaximumLength(150);
        RuleFor(x => x.SourceType).MaximumLength(50);
        RuleFor(x => x.SourceCode).MaximumLength(100);
        RuleFor(x => x.ApprovedByUser).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(2000);

        // ReturnableOut requires an ExpectedReturnDate
        RuleFor(x => x.ExpectedReturnDate)
            .NotNull()
            .When(x => x.Type == nameof(GatePassType.ReturnableOut))
            .WithMessage("A Returnable Out gate pass must have an Expected Return Date.");

        // Visitor pass requires VisitorName
        RuleFor(x => x.VisitorName)
            .NotEmpty()
            .When(x => x.Type == nameof(GatePassType.Visitor))
            .WithMessage("Visitor gate pass needs a visitor name.");
    }
}

internal sealed class CreateGatePassCommandHandler
    : IRequestHandler<CreateGatePassCommand, ApiResponse<long>>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly ICurrentUserService _currentUser;

    public CreateGatePassCommandHandler(
        IRepository<Domain.Entities.GatePass, long> repo,
        IUnitOfWork uow,
        INumberingService numbering,
        ICurrentUserService currentUser)
    {
        _repo = repo; _uow = uow; _numbering = numbering; _currentUser = currentUser;
    }

    public async Task<ApiResponse<long>> Handle(CreateGatePassCommand cmd, CancellationToken ct)
    {
        var code = await _numbering.NextAsync("GP", null, ct);
        var entity = new Domain.Entities.GatePass
        {
            Code = code,
            PassDate = cmd.PassDate,
            PassTime = cmd.PassTime,
            Type = Enum.Parse<GatePassType>(cmd.Type),
            Direction = Enum.Parse<GatePassDirection>(cmd.Direction),
            VehicleNumber = T(cmd.VehicleNumber),
            DriverName = T(cmd.DriverName),
            DriverPhone = T(cmd.DriverPhone),
            DriverNidNumber = T(cmd.DriverNidNumber),
            TransporterName = T(cmd.TransporterName),
            VisitorName = T(cmd.VisitorName),
            VisitorPhone = T(cmd.VisitorPhone),
            VisitorOrganization = T(cmd.VisitorOrganization),
            VisitorPurpose = T(cmd.VisitorPurpose),
            ItemDescription = T(cmd.ItemDescription),
            Quantity = T(cmd.Quantity),
            FromLocation = T(cmd.FromLocation),
            ToLocation = T(cmd.ToLocation),
            SourceType = T(cmd.SourceType),
            SourceId = cmd.SourceId,
            SourceCode = T(cmd.SourceCode),
            IssuedByUser = _currentUser.UserName ?? "system",
            ApprovedByUser = T(cmd.ApprovedByUser),
            ExpectedReturnDate = cmd.ExpectedReturnDate,
            Status = GatePassStatus.Open,
            Notes = T(cmd.Notes)
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(entity.Id, $"Gate pass {entity.Code} issued.");
    }

    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ───────────────────────────────────────────────────────────────────────────
//   Update (Open only)
// ───────────────────────────────────────────────────────────────────────────
public sealed record UpdateGatePassCommand(
    long Id,
    DateOnly PassDate,
    TimeOnly? PassTime,
    string Type,
    string Direction,
    string? VehicleNumber,
    string? DriverName,
    string? DriverPhone,
    string? DriverNidNumber,
    string? TransporterName,
    string? VisitorName,
    string? VisitorPhone,
    string? VisitorOrganization,
    string? VisitorPurpose,
    string? ItemDescription,
    string? Quantity,
    string? FromLocation,
    string? ToLocation,
    string? SourceType,
    long? SourceId,
    string? SourceCode,
    string? ApprovedByUser,
    DateOnly? ExpectedReturnDate,
    string? Notes
) : IRequest<ApiResponse>;

public sealed class UpdateGatePassCommandValidator : AbstractValidator<UpdateGatePassCommand>
{
    public UpdateGatePassCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Type).Must(v => Enum.TryParse<GatePassType>(v, out _));
        RuleFor(x => x.Direction).Must(v => Enum.TryParse<GatePassDirection>(v, out _));
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateGatePassCommandHandler
    : IRequestHandler<UpdateGatePassCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateGatePassCommandHandler(IRepository<Domain.Entities.GatePass, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(UpdateGatePassCommand cmd, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(cmd.Id, ct);
        if (g is null) return ApiResponse.Fail("Gate pass not found.");
        if (g.Status != GatePassStatus.Open)
            return ApiResponse.Fail($"Cannot edit a {g.Status} gate pass.");

        g.PassDate = cmd.PassDate;
        g.PassTime = cmd.PassTime;
        g.Type = Enum.Parse<GatePassType>(cmd.Type);
        g.Direction = Enum.Parse<GatePassDirection>(cmd.Direction);
        g.VehicleNumber = T(cmd.VehicleNumber);
        g.DriverName = T(cmd.DriverName);
        g.DriverPhone = T(cmd.DriverPhone);
        g.DriverNidNumber = T(cmd.DriverNidNumber);
        g.TransporterName = T(cmd.TransporterName);
        g.VisitorName = T(cmd.VisitorName);
        g.VisitorPhone = T(cmd.VisitorPhone);
        g.VisitorOrganization = T(cmd.VisitorOrganization);
        g.VisitorPurpose = T(cmd.VisitorPurpose);
        g.ItemDescription = T(cmd.ItemDescription);
        g.Quantity = T(cmd.Quantity);
        g.FromLocation = T(cmd.FromLocation);
        g.ToLocation = T(cmd.ToLocation);
        g.SourceType = T(cmd.SourceType);
        g.SourceId = cmd.SourceId;
        g.SourceCode = T(cmd.SourceCode);
        g.ApprovedByUser = T(cmd.ApprovedByUser);
        g.ExpectedReturnDate = cmd.ExpectedReturnDate;
        g.Notes = T(cmd.Notes);

        _repo.Update(g);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Gate pass updated.");
    }

    private static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ───────────────────────────────────────────────────────────────────────────
//   Close (Non-returnable / Visitor / Vehicle / InwardReceipt → Closed)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CloseGatePassCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CloseGatePassCommandHandler : IRequestHandler<CloseGatePassCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;
    public CloseGatePassCommandHandler(IRepository<Domain.Entities.GatePass, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(CloseGatePassCommand cmd, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(cmd.Id, ct);
        if (g is null) return ApiResponse.Fail("Gate pass not found.");
        if (g.Status != GatePassStatus.Open)
            return ApiResponse.Fail($"Gate pass is already {g.Status}.");
        if (g.Type == GatePassType.ReturnableOut)
            return ApiResponse.Fail("Returnable Out passes must be closed via Mark Returned.");
        g.Status = GatePassStatus.Closed;
        g.ClosedAt = DateTimeOffset.UtcNow;
        _repo.Update(g);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Gate pass {g.Code} closed.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Mark Returned (ReturnableOut → Returned)
// ───────────────────────────────────────────────────────────────────────────
public sealed record MarkGatePassReturnedCommand(long Id, string? ReturnNotes) : IRequest<ApiResponse>;

public sealed class MarkGatePassReturnedCommandValidator : AbstractValidator<MarkGatePassReturnedCommand>
{
    public MarkGatePassReturnedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.ReturnNotes).MaximumLength(1000);
    }
}

internal sealed class MarkGatePassReturnedCommandHandler
    : IRequestHandler<MarkGatePassReturnedCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    public MarkGatePassReturnedCommandHandler(
        IRepository<Domain.Entities.GatePass, long> repo, IUnitOfWork uow, ICurrentUserService currentUser)
    { _repo = repo; _uow = uow; _currentUser = currentUser; }

    public async Task<ApiResponse> Handle(MarkGatePassReturnedCommand cmd, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(cmd.Id, ct);
        if (g is null) return ApiResponse.Fail("Gate pass not found.");
        if (g.Type != GatePassType.ReturnableOut)
            return ApiResponse.Fail("Only Returnable Out passes can be marked returned.");
        if (g.Status != GatePassStatus.Open)
            return ApiResponse.Fail($"Gate pass is already {g.Status}.");

        g.Status = GatePassStatus.Returned;
        g.ReturnedAt = DateTimeOffset.UtcNow;
        g.ReturnedByUser = _currentUser.UserName ?? "system";
        g.ReturnNotes = string.IsNullOrWhiteSpace(cmd.ReturnNotes) ? null : cmd.ReturnNotes.Trim();
        _repo.Update(g);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Gate pass {g.Code} marked returned.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Cancel (Open → Cancelled)
// ───────────────────────────────────────────────────────────────────────────
public sealed record CancelGatePassCommand(long Id) : IRequest<ApiResponse>;

internal sealed class CancelGatePassCommandHandler : IRequestHandler<CancelGatePassCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;
    public CancelGatePassCommandHandler(IRepository<Domain.Entities.GatePass, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(CancelGatePassCommand cmd, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(cmd.Id, ct);
        if (g is null) return ApiResponse.Fail("Gate pass not found.");
        if (g.Status != GatePassStatus.Open)
            return ApiResponse.Fail($"Gate pass is already {g.Status}.");
        g.Status = GatePassStatus.Cancelled;
        _repo.Update(g);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok($"Gate pass {g.Code} cancelled.");
    }
}

// ───────────────────────────────────────────────────────────────────────────
//   Delete (Open only — soft delete)
// ───────────────────────────────────────────────────────────────────────────
public sealed record DeleteGatePassCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteGatePassCommandHandler : IRequestHandler<DeleteGatePassCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.GatePass, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteGatePassCommandHandler(IRepository<Domain.Entities.GatePass, long> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteGatePassCommand cmd, CancellationToken ct)
    {
        var g = await _repo.GetByIdAsync(cmd.Id, ct);
        if (g is null) return ApiResponse.Fail("Gate pass not found.");
        if (g.Status != GatePassStatus.Open)
            return ApiResponse.Fail($"Cannot delete a {g.Status} gate pass. Cancel it first.");
        _repo.Remove(g);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Gate pass deleted.");
    }
}
