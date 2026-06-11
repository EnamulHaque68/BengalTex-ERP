using BengalTex.ERP.Application.CustomerInvoice.Dtos;
using BengalTex.ERP.Application.CustomerInvoice.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.CustomerInvoice.Commands;

/// <summary>
/// Sets the BD export-reporting + shipping-document fields on a non-Draft, non-Cancelled
/// CustomerInvoice. Form-EXP / LC / ShipmentDate are for EPB Form-N audit reporting;
/// IncoTerm / ports / vessel / packages / weights / shipping marks drive the
/// Commercial Invoice + Packing List printables. Editable at any post-Draft stage —
/// these fields don't affect the invoice's own AR lifecycle.
/// </summary>
public sealed record MarkInvoiceAsExportedCommand(
    long Id,
    string? EpbFormNumber,
    string? LcNumber,
    DateOnly? ShipmentDate,
    string? IncoTerm,
    string? PortOfLoading,
    string? PortOfDischarge,
    string? VesselName,
    string? CountryOfDestination,
    string? ShippingMarks,
    int? TotalPackages,
    decimal? GrossWeightKg,
    decimal? NetWeightKg,
    string? ContainerNumber,
    string? SealNumber,
    string? TruckNumber,
    int? BeneficiaryBankAccountId
) : IRequest<ApiResponse<CustomerInvoiceDto>>;

public sealed class MarkInvoiceAsExportedCommandValidator : AbstractValidator<MarkInvoiceAsExportedCommand>
{
    public MarkInvoiceAsExportedCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.EpbFormNumber).MaximumLength(50);
        RuleFor(x => x.LcNumber).MaximumLength(50);
        RuleFor(x => x.IncoTerm).MaximumLength(20);
        RuleFor(x => x.PortOfLoading).MaximumLength(100);
        RuleFor(x => x.PortOfDischarge).MaximumLength(100);
        RuleFor(x => x.VesselName).MaximumLength(100);
        RuleFor(x => x.CountryOfDestination).MaximumLength(100);
        RuleFor(x => x.ShippingMarks).MaximumLength(1000);
        RuleFor(x => x.TotalPackages).GreaterThanOrEqualTo(0).When(x => x.TotalPackages.HasValue);
        RuleFor(x => x.GrossWeightKg).GreaterThanOrEqualTo(0).When(x => x.GrossWeightKg.HasValue);
        RuleFor(x => x.NetWeightKg).GreaterThanOrEqualTo(0).When(x => x.NetWeightKg.HasValue);
        RuleFor(x => x.ContainerNumber).MaximumLength(50);
        RuleFor(x => x.SealNumber).MaximumLength(50);
        RuleFor(x => x.TruckNumber).MaximumLength(50);
    }
}

internal sealed class MarkInvoiceAsExportedCommandHandler
    : IRequestHandler<MarkInvoiceAsExportedCommand, ApiResponse<CustomerInvoiceDto>>
{
    private readonly IRepository<Domain.Entities.CustomerInvoice, long> _repo;
    private readonly IRepository<Domain.Entities.BankAccount> _bankRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public MarkInvoiceAsExportedCommandHandler(
        IRepository<Domain.Entities.CustomerInvoice, long> repo,
        IRepository<Domain.Entities.BankAccount> bankRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo; _bankRepo = bankRepo; _uow = uow; _mediator = mediator;
    }

    public async Task<ApiResponse<CustomerInvoiceDto>> Handle(MarkInvoiceAsExportedCommand cmd, CancellationToken ct)
    {
        var inv = await _repo.GetByIdAsync(cmd.Id, ct);
        if (inv is null) return ApiResponse<CustomerInvoiceDto>.Fail("Invoice not found.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Draft)
            return ApiResponse<CustomerInvoiceDto>.Fail("Issue the invoice first before marking it as exported.");
        if (inv.Status == Domain.Entities.CustomerInvoiceStatus.Cancelled)
            return ApiResponse<CustomerInvoiceDto>.Fail("Cannot record export details on a cancelled invoice.");

        if (cmd.BeneficiaryBankAccountId.HasValue
            && await _bankRepo.GetByIdAsync(cmd.BeneficiaryBankAccountId.Value, ct) is null)
            return ApiResponse<CustomerInvoiceDto>.Fail("Beneficiary bank account not found.");

        inv.EpbFormNumber = Trim(cmd.EpbFormNumber);
        inv.LcNumber = Trim(cmd.LcNumber);
        inv.ShipmentDate = cmd.ShipmentDate;
        inv.IncoTerm = Trim(cmd.IncoTerm);
        inv.PortOfLoading = Trim(cmd.PortOfLoading);
        inv.PortOfDischarge = Trim(cmd.PortOfDischarge);
        inv.VesselName = Trim(cmd.VesselName);
        inv.CountryOfDestination = Trim(cmd.CountryOfDestination);
        inv.ShippingMarks = Trim(cmd.ShippingMarks);
        inv.TotalPackages = cmd.TotalPackages;
        inv.GrossWeightKg = cmd.GrossWeightKg;
        inv.NetWeightKg = cmd.NetWeightKg;
        inv.ContainerNumber = Trim(cmd.ContainerNumber);
        inv.SealNumber = Trim(cmd.SealNumber);
        inv.TruckNumber = Trim(cmd.TruckNumber);
        inv.BeneficiaryBankAccountId = cmd.BeneficiaryBankAccountId;

        _repo.Update(inv);
        await _uow.SaveChangesAsync(ct);

        return await _mediator.Send(new GetCustomerInvoiceByIdQuery(cmd.Id), ct);
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
