using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Application.SupplierInvoice.Dtos;
using BengalTex.ERP.Application.SupplierInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.SupplierInvoice.Commands;

/// <summary>
/// Approves a Draft supplier invoice — locks lines/total and authorizes it for
/// payment. Lifecycle: Draft → Approved. Once Approved, lines cannot be edited;
/// only AmountPaid/Status change via Payment create/delete. Mirror of
/// <c>IssueCustomerInvoiceCommand</c>; called "Approve" here because we're
/// internally authorizing the supplier's bill (we don't "issue" it).
/// </summary>
public sealed record ApproveSupplierInvoiceCommand(long Id) : IRequest<ApiResponse<SupplierInvoiceDto>>;

internal sealed class ApproveSupplierInvoiceCommandHandler
    : IRequestHandler<ApproveSupplierInvoiceCommand, ApiResponse<SupplierInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.SupplierInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUserService _currentUser;
    private readonly IMediator _mediator;

    public ApproveSupplierInvoiceCommandHandler(
        IRepository<Domain.Entities.SupplierInvoice, long> repo,
        IUnitOfWork uow,
        ICurrentUserService currentUser,
        IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _currentUser = currentUser;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SupplierInvoiceDto>> Handle(
        ApproveSupplierInvoiceCommand cmd, CancellationToken cancellationToken)
    {
        var inv = await _repo.Query()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == cmd.Id, cancellationToken);
        if (inv is null) return ApiResponse<SupplierInvoiceDto>.Fail("Supplier invoice not found.");
        if (inv.Status != Domain.Entities.SupplierInvoiceStatus.Draft)
            return ApiResponse<SupplierInvoiceDto>.Fail("Only draft supplier invoices can be approved.");
        if (inv.Lines.Count == 0)
            return ApiResponse<SupplierInvoiceDto>.Fail("Cannot approve an invoice with no lines.");

        inv.TotalAmount = inv.Lines.Sum(l => l.Quantity * l.UnitPrice);

        inv.Status = Domain.Entities.SupplierInvoiceStatus.Approved;
        inv.ApprovedAt = DateTimeOffset.UtcNow;
        inv.ApprovedBy = _currentUser.UserName;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(cancellationToken);

        return await _mediator.Send(new GetSupplierInvoiceByIdQuery(inv.Id), cancellationToken);
    }
}
