using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Settings;
using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>
/// Confirms a draft SO — customer commitment locked in. Lifecycle: Draft → Confirmed.
/// Enforces credit control (when enabled): the customer's total exposure — outstanding
/// AR (BDT) plus this order's value — may not exceed their CreditLimit. A CreditLimit of
/// 0 means "no limit set" and is not enforced.
/// </summary>
public sealed record ConfirmSalesOrderCommand(long Id) : IRequest<ApiResponse<SalesOrderDto>>;

internal sealed class ConfirmSalesOrderCommandHandler
    : IRequestHandler<ConfirmSalesOrderCommand, ApiResponse<SalesOrderDto>>
{
    private readonly IRepository<Domain.Entities.SalesOrder, long> _repo;
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _invRepo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;
    private readonly CreditControlSettings _credit;

    public ConfirmSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator,
        IOptions<CreditControlSettings> credit)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
        _credit = credit.Value;
    }

    public async Task<ApiResponse<SalesOrderDto>> Handle(
        ConfirmSalesOrderCommand cmd, CancellationToken cancellationToken)
    {
        var so = await _repo.Query()
            .Include(s => s.Lines)
            .Include(s => s.Customer)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);
        if (so is null) return ApiResponse<SalesOrderDto>.Fail("Sales order not found.");
        if (so.Status != Domain.Entities.SalesOrderStatus.Draft)
            return ApiResponse<SalesOrderDto>.Fail("Only draft sales orders can be confirmed.");

        // ── Credit control ──────────────────────────────────────────────────
        // Enforced only when enabled, set to block, and the customer carries a positive limit.
        if (_credit.Enabled && _credit.BlockOverLimit && so.Customer.CreditLimit > 0m)
        {
            // Current outstanding AR in BDT (Issued / PartiallyPaid invoices with a balance).
            var outstanding = await _invRepo.Query()
                .Where(i => i.CustomerId == so.CustomerId
                         && (i.Status == CustomerInvoiceStatus.Issued || i.Status == CustomerInvoiceStatus.PartiallyPaid)
                         && i.TotalAmount - i.AmountPaid > 0m)
                .SumAsync(i => (decimal?)((i.TotalAmount - i.AmountPaid) * i.ExchangeRate), cancellationToken) ?? 0m;

            // Value of the order being confirmed, in BDT (line totals are net; credit is gross-ish
            // but net is a fair, conservative proxy and avoids re-deriving VAT here).
            var orderValue = so.Lines.Sum(l => l.Quantity * l.UnitPrice) * so.ExchangeRate;
            var exposure = outstanding + orderValue;

            if (exposure > so.Customer.CreditLimit)
            {
                var over = exposure - so.Customer.CreditLimit;
                return ApiResponse<SalesOrderDto>.Fail(
                    $"Credit limit exceeded for {so.Customer.Name}. " +
                    $"Limit {so.Customer.CreditLimit:N0} BDT; current outstanding {outstanding:N0} + this order " +
                    $"{orderValue:N0} = {exposure:N0} (over by {over:N0}). " +
                    "Take a payment, close older invoices, or raise the customer's credit limit before confirming.");
            }
        }

        so.Status = Domain.Entities.SalesOrderStatus.Confirmed;
        so.ConfirmedAt = DateTimeOffset.UtcNow;
        so.ConfirmedBy = _currentUser.UserName;
        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
