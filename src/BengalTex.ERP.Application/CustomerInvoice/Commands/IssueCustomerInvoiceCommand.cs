using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Issues a Draft customer invoice — locks the lines/total and makes it eligible for
/// receipts. Lifecycle: Draft → Issued. Once Issued, lines cannot be edited; only
/// AmountPaid/Status change via Receipt create/delete.
/// </summary>
public sealed record IssueCustomerInvoiceCommand(long Id) : IRequest<ApiResponse<CustomerInvoiceDto>>;

internal sealed class IssueCustomerInvoiceCommandHandler
    : IRequestHandler<IssueCustomerInvoiceCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public IssueCustomerInvoiceCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(
        IssueCustomerInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(c => c.Lines)
            .FirstOrDefaultAsync(c => c.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Customer invoice not found.");
        if (inv.Status != Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Only draft customer invoices can be issued.");
        if (inv.Lines.Count == 0)
            return ApiResponse<CustomerInvoiceDto>.Fail("Cannot issue an invoice with no lines.");

        // Snapshot total at issue time (already kept in sync by Create/Update, but recompute defensively)
        inv.TotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);

        inv.Status = Domain.Entities.CustomerInvoiceStatus.Issued;
        inv.IssuedAt = DateTimeOffset.UtcNow;
        inv.IssuedBy = _currentUser.UserName;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
