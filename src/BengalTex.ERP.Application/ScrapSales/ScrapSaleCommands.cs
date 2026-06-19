using BengalTex.ERP.Application.Accounting;
using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ScrapSales;

internal static class ScrapSaleRules
{
    public static void Apply<T>(AbstractValidator<T> v, Func<T, IReadOnlyList<ScrapSaleLineInput>> lines)
    {
        v.RuleFor(x => lines(x)).NotEmpty().WithMessage("Add at least one line.");
        v.RuleForEach(x => lines(x)).ChildRules(l =>
        {
            l.RuleFor(i => i.Description).NotEmpty().MaximumLength(300);
            l.RuleFor(i => i.Quantity).GreaterThan(0);
            l.RuleFor(i => i.UnitPrice).GreaterThanOrEqualTo(0);
            l.RuleFor(i => i.Unit).MaximumLength(20);
        });
    }

    public static List<ScrapSaleLine> Build(IReadOnlyList<ScrapSaleLineInput> lines) =>
        lines.Select((l, i) => new ScrapSaleLine
        {
            Description = l.Description.Trim(),
            Quantity = l.Quantity,
            Unit = string.IsNullOrWhiteSpace(l.Unit) ? null : l.Unit.Trim(),
            UnitPrice = l.UnitPrice,
            SortOrder = i
        }).ToList();
}

// ── Create ──
public sealed record CreateScrapSaleCommand(
    DateOnly SaleDate, string? BuyerName, string PaymentMethod, string? Notes, IReadOnlyList<ScrapSaleLineInput> Lines)
    : IRequest<ApiResponse<long>>;

public sealed class CreateScrapSaleCommandValidator : AbstractValidator<CreateScrapSaleCommand>
{
    public CreateScrapSaleCommandValidator()
    {
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.BuyerName).MaximumLength(200);
        RuleFor(x => x.PaymentMethod).Must(p => Enum.TryParse<PaymentMethod>(p, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        ScrapSaleRules.Apply(this, x => x.Lines);
    }
}

internal sealed class CreateScrapSaleCommandHandler : IRequestHandler<CreateScrapSaleCommand, ApiResponse<long>>
{
    private readonly IRepository<ScrapSale, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    public CreateScrapSaleCommandHandler(IRepository<ScrapSale, long> repo, IUnitOfWork uow, INumberingService numbering)
    { _repo = repo; _uow = uow; _numbering = numbering; }

    public async Task<ApiResponse<long>> Handle(CreateScrapSaleCommand cmd, CancellationToken ct)
    {
        var e = new ScrapSale
        {
            Code = await _numbering.NextAsync("SCRAP", null, ct),
            SaleDate = cmd.SaleDate,
            BuyerName = string.IsNullOrWhiteSpace(cmd.BuyerName) ? null : cmd.BuyerName.Trim(),
            PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod),
            Status = ScrapSaleStatus.Draft,
            Notes = cmd.Notes,
            Lines = ScrapSaleRules.Build(cmd.Lines)
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Scrap sale created.");
    }
}

// ── Update (draft only) ──
public sealed record UpdateScrapSaleCommand(
    long Id, DateOnly SaleDate, string? BuyerName, string PaymentMethod, string? Notes, IReadOnlyList<ScrapSaleLineInput> Lines)
    : IRequest<ApiResponse<long>>;

public sealed class UpdateScrapSaleCommandValidator : AbstractValidator<UpdateScrapSaleCommand>
{
    public UpdateScrapSaleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.SaleDate).NotEmpty();
        RuleFor(x => x.BuyerName).MaximumLength(200);
        RuleFor(x => x.PaymentMethod).Must(p => Enum.TryParse<PaymentMethod>(p, out _)).WithMessage("Invalid payment method.");
        RuleFor(x => x.Notes).MaximumLength(2000);
        ScrapSaleRules.Apply(this, x => x.Lines);
    }
}

internal sealed class UpdateScrapSaleCommandHandler : IRequestHandler<UpdateScrapSaleCommand, ApiResponse<long>>
{
    private readonly IRepository<ScrapSale, long> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateScrapSaleCommandHandler(IRepository<ScrapSale, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(UpdateScrapSaleCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Scrap sale not found.");
        if (e.Status != ScrapSaleStatus.Draft) return ApiResponse<long>.Fail("Only draft scrap sales can be edited.");

        e.SaleDate = cmd.SaleDate;
        e.BuyerName = string.IsNullOrWhiteSpace(cmd.BuyerName) ? null : cmd.BuyerName.Trim();
        e.PaymentMethod = Enum.Parse<PaymentMethod>(cmd.PaymentMethod);
        e.Notes = cmd.Notes;
        e.Lines.Clear();
        foreach (var l in ScrapSaleRules.Build(cmd.Lines)) e.Lines.Add(l);

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Scrap sale updated.");
    }
}

// ── Delete (draft only) ──
public sealed record DeleteScrapSaleCommand(long Id) : IRequest<ApiResponse>;

internal sealed class DeleteScrapSaleCommandHandler : IRequestHandler<DeleteScrapSaleCommand, ApiResponse>
{
    private readonly IRepository<ScrapSale, long> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteScrapSaleCommandHandler(IRepository<ScrapSale, long> repo, IUnitOfWork uow) { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteScrapSaleCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Scrap sale not found.");
        if (e.Status != ScrapSaleStatus.Draft) return ApiResponse.Fail("Only draft scrap sales can be deleted.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Scrap sale deleted.");
    }
}

// ── Post (journals the income) ──
public sealed record PostScrapSaleCommand(long Id) : IRequest<ApiResponse<long>>;

internal sealed class PostScrapSaleCommandHandler : IRequestHandler<PostScrapSaleCommand, ApiResponse<long>>
{
    private readonly IRepository<ScrapSale, long> _repo;
    private readonly IJournalPostingService _journal;
    private readonly ICurrentUserService _currentUser;
    private readonly IUnitOfWork _uow;

    public PostScrapSaleCommandHandler(
        IRepository<ScrapSale, long> repo, IJournalPostingService journal,
        ICurrentUserService currentUser, IUnitOfWork uow)
    { _repo = repo; _journal = journal; _currentUser = currentUser; _uow = uow; }

    public async Task<ApiResponse<long>> Handle(PostScrapSaleCommand cmd, CancellationToken ct)
    {
        var e = await _repo.Query().Include(x => x.Lines).FirstOrDefaultAsync(x => x.Id == cmd.Id, ct);
        if (e is null) return ApiResponse<long>.Fail("Scrap sale not found.");
        if (e.Status != ScrapSaleStatus.Draft) return ApiResponse<long>.Fail("Only draft scrap sales can be posted.");
        if (e.Lines.Count == 0) return ApiResponse<long>.Fail("Cannot post a scrap sale with no lines.");

        var total = e.Lines.Sum(l => l.Quantity * l.UnitPrice);
        if (total > 0m)
        {
            var cashAccount = e.PaymentMethod == PaymentMethod.Cash ? LedgerAccounts.Cash : LedgerAccounts.Bank;
            await _journal.PostAsync(
                e.SaleDate, $"Scrap sale {e.Code}" + (e.BuyerName is null ? "" : $" — {e.BuyerName}"),
                "ScrapSale", e.Id, e.Code,
                new[]
                {
                    new JournalPostingLine(cashAccount, total, 0m),
                    new JournalPostingLine(LedgerAccounts.ScrapSalesIncome, 0m, total),
                }, ct);
        }

        e.Status = ScrapSaleStatus.Posted;
        e.PostedAt = DateTimeOffset.UtcNow;
        e.PostedBy = _currentUser.UserName;
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<long>.Ok(e.Id, "Scrap sale posted.");
    }
}
