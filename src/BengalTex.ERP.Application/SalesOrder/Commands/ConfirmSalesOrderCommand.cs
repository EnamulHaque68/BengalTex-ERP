using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.Common.Settings;
using BengalTex.ERP.Application.SalesOrder.Dtos;
using BengalTex.ERP.Application.SalesOrder.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BengalTex.ERP.Application.SalesOrder.Commands;

/// <summary>
/// Confirms a draft SO — customer commitment locked in. First enforces credit control (when
/// enabled): the customer's exposure (outstanding AR + this order's value) may not exceed their
/// CreditLimit. Then routes through the approval workflow: within the SO threshold it is
/// auto-confirmed (Draft → Confirmed); above it, a sign-off request is raised
/// (Draft → PendingApproval), cleared from the Approvals inbox.
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
    private readonly IApprovalService _approval;
    private readonly INotificationService _notifications;

    public ConfirmSalesOrderCommandHandler(
        IRepository<Domain.Entities.SalesOrder, long> repo,
        IRepository<Domain.Entities.CustomerInvoice, long> invRepo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator,
        IOptions<CreditControlSettings> credit,
        IApprovalService approval,
        INotificationService notifications)
    {
        _repo = repo;
        _invRepo = invRepo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
        _credit = credit.Value;
        _approval = approval;
        _notifications = notifications;
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

        // Order value in base BDT (line totals are net; a fair proxy that avoids re-deriving VAT).
        var orderValue = so.Lines.Sum(l => l.Quantity * l.UnitPrice) * so.ExchangeRate;

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

        // ── Approval gate ────────────────────────────────────────────────────
        // Within threshold → auto-confirm; above → raise a pending request (Approvals inbox).
        var approval = await _approval.SubmitAsync("SalesOrder", so.Id, so.Code, orderValue, cancellationToken);
        if (approval.AutoApproved)
        {
            so.Status = Domain.Entities.SalesOrderStatus.Confirmed;
            so.ConfirmedAt = DateTimeOffset.UtcNow;
            so.ConfirmedBy = _currentUser.UserName;

            // Phase 7 — smart notification: confirmed SO is ready for production planning.
            await _notifications.NotifyAsync(
                NotificationChannels.InApp, NotificationRecipients.ProductionTeam,
                $"Sales order {so.Code} confirmed",
                $"{so.Customer.Name} — ready for production planning.",
                "SalesOrder", so.Id, cancellationToken);
        }
        else
        {
            so.Status = Domain.Entities.SalesOrderStatus.PendingApproval;
        }

        _repo.Update(so);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSalesOrderByIdQuery(so.Id), cancellationToken);
    }
}
