using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Sets the BD export-reporting fields on a non-Draft, non-Cancelled CustomerInvoice:
/// Form-EXP number (issued by bank for FX repatriation), LC reference, and physical
/// shipment date. Editable at any post-Draft stage — the fields are informational
/// for EPB Form-N reporting and don't affect the invoice's own lifecycle.
/// </summary>
public sealed record MarkInvoiceAsExportedCommand(
    long Id,
    string? EpbFormNumber,
    string? LcNumber,
    DateOnly? ShipmentDate
) : IRequest<ApiResponse<CustomerInvoiceDto>>;

public sealed class MarkInvoiceAsExportedCommandValidator : AbstractValidator<MarkInvoiceAsExportedCommand>
{
    public MarkInvoiceAsExportedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EpbFormNumber).MaximumLength(50);
        RuleFor(x => x.LcNumber).MaximumLength(50);
    }
}

internal sealed class MarkInvoiceAsExportedCommandHandler
    : IRequestHandler<MarkInvoiceAsExportedCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public MarkInvoiceAsExportedCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(MarkInvoiceAsExportedCommand cmd, CancellationToken ct)
    {
        var inv = await _repo.GetByIdAsync(cmd.Id, ct);
        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Invoice not found.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Issue the invoice first before marking it as exported.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Cancelled)
            return ApiResponse<CustomerInvoiceDto>.Fail("Cannot record export details on a cancelled invoice.");

        inv.EpbFormNumber = string.IsNullOrWhiteSpace(cmd.EpbFormNumber) ? null : cmd.EpbFormNumber.Trim();
        inv.LcNumber = string.IsNullOrWhiteSpace(cmd.LcNumber) ? null : cmd.LcNumber.Trim();
        inv.ShipmentDate = cmd.ShipmentDate;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(cmd.Id), ct);
    }
}
