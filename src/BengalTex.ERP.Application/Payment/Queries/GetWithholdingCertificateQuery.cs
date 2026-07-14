using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Payment.Queries;

/// <summary>Phase A5b — data for the printable AIT/VDS withholding certificate issued to a supplier.</summary>
public sealed record WithholdingCertificateDto(
    long PaymentId, string PaymentCode, DateOnly PaymentDate,
    string SupplierInvoiceCode, string CurrencyCode, decimal ExchangeRate,
    decimal GrossBdt, decimal AitAmount, decimal VdsAmount, decimal NetPaidBdt,
    string PaymentMethod, string? ReferenceNumber,
    // Supplier (deductee)
    string SupplierName, string? SupplierAddress, string? SupplierBin, string? SupplierTin, string? SupplierPhone,
    // Company (withholding agent)
    string CompanyName, string? CompanyAddress, string? CompanyBin, string? CompanyTin);

public sealed record GetWithholdingCertificateQuery(long PaymentId) : IRequest<ApiResponse<WithholdingCertificateDto>>;

internal sealed class GetWithholdingCertificateQueryHandler
    : IRequestHandler<GetWithholdingCertificateQuery, ApiResponse<WithholdingCertificateDto>>
{
    private readonly IRepository<Domain.Entities.Payment, long> _repo;
    private readonly IRepository<Domain.Entities.Company> _companyRepo;

    public GetWithholdingCertificateQueryHandler(
        IRepository<Domain.Entities.Payment, long> repo, IRepository<Domain.Entities.Company> companyRepo)
    {
        _repo = repo; _companyRepo = companyRepo;
    }

    public async Task<ApiResponse<WithholdingCertificateDto>> Handle(GetWithholdingCertificateQuery q, CancellationToken ct)
    {
        var pay = await _repo.Query().AsNoTracking()
            .Include(p => p.SupplierInvoice).ThenInclude(s => s.Supplier)
            .Include(p => p.SupplierInvoice).ThenInclude(s => s.Currency)
            .FirstOrDefaultAsync(p => p.Id == q.PaymentId, ct);
        if (pay is null) return ApiResponse<WithholdingCertificateDto>.Fail("Payment not found.");
        if (pay.AitAmount <= 0m && pay.VdsAmount <= 0m)
            return ApiResponse<WithholdingCertificateDto>.Fail("This payment has no tax withheld — no certificate to issue.");

        var inv = pay.SupplierInvoice;
        var sup = inv.Supplier;
        var company = await _companyRepo.Query().AsNoTracking().FirstOrDefaultAsync(ct);

        var grossBdt = Math.Round(pay.Amount * pay.ExchangeRate, 2);
        var net = Math.Round(grossBdt - pay.AitAmount - pay.VdsAmount, 2);

        var supAddress = string.Join(", ",
            new[] { sup.AddressLine1, sup.AddressLine2, sup.City }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var coAddress = company is null ? null : string.Join(", ",
            new[] { company.AddressLine1, company.AddressLine2, company.City }.Where(s => !string.IsNullOrWhiteSpace(s)));

        var dto = new WithholdingCertificateDto(
            pay.Id, pay.Code, pay.PaymentDate,
            inv.Code, inv.Currency.Code, pay.ExchangeRate,
            grossBdt, pay.AitAmount, pay.VdsAmount, net,
            pay.PaymentMethod.ToString(), pay.ReferenceNumber,
            sup.Name, string.IsNullOrWhiteSpace(supAddress) ? null : supAddress, sup.BinNumber, sup.TinNumber, sup.Phone,
            company?.Name ?? "Bengal TEX", coAddress, company?.TaxNumber, company?.TaxNumber);

        return ApiResponse<WithholdingCertificateDto>.Ok(dto);
    }
}
