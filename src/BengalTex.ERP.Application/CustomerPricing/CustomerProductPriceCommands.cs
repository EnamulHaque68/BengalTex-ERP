using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.CustomerPricing;

internal static class CustomerPricingText
{
    /// <summary>Trim to null — empty/whitespace becomes null.</summary>
    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ── DTOs ──
public sealed record CustomerProductPriceDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int ProductId,
    string ProductCode,
    string ProductName,
    string ProductUnitOfMeasureCode,
    decimal DefaultSalesPrice,     // the product's standard price, for reference
    decimal UnitPrice,             // this customer's negotiated price
    string? BuyerProductCode,      // buyer's own article code (export docs)
    string? BuyerProductName,
    bool IsActive,
    string? Notes);

// ── List by customer ──
public sealed record GetCustomerProductPricesQuery(int CustomerId, bool IncludeInactive = false)
    : IRequest<ApiResponse<IReadOnlyList<CustomerProductPriceDto>>>;

internal sealed class GetCustomerProductPricesQueryHandler
    : IRequestHandler<GetCustomerProductPricesQuery, ApiResponse<IReadOnlyList<CustomerProductPriceDto>>>
{
    private readonly IRepository<CustomerProductPrice> _repo;
    public GetCustomerProductPricesQueryHandler(IRepository<CustomerProductPrice> repo) => _repo = repo;

    public async Task<ApiResponse<IReadOnlyList<CustomerProductPriceDto>>> Handle(
        GetCustomerProductPricesQuery req, CancellationToken ct)
    {
        var q = _repo.Query().Where(p => p.CustomerId == req.CustomerId);
        if (!req.IncludeInactive) q = q.Where(p => p.IsActive);
        var items = await q
            .OrderBy(p => p.Product.Name)
            .Select(p => new CustomerProductPriceDto(
                p.Id, p.CustomerId, p.Customer.Name,
                p.ProductId, p.Product.Code, p.Product.Name, p.Product.UnitOfMeasure.Code,
                p.Product.SalesPrice, p.UnitPrice, p.BuyerProductCode, p.BuyerProductName, p.IsActive, p.Notes))
            .ToListAsync(ct);
        return ApiResponse<IReadOnlyList<CustomerProductPriceDto>>.Ok(items);
    }
}

// ── Price lookup (used by SO / Quotation forms to auto-fill a line) ──
public sealed record GetCustomerProductPriceQuery(int CustomerId, int ProductId)
    : IRequest<ApiResponse<decimal?>>;

internal sealed class GetCustomerProductPriceQueryHandler
    : IRequestHandler<GetCustomerProductPriceQuery, ApiResponse<decimal?>>
{
    private readonly IRepository<CustomerProductPrice> _repo;
    public GetCustomerProductPriceQueryHandler(IRepository<CustomerProductPrice> repo) => _repo = repo;

    public async Task<ApiResponse<decimal?>> Handle(GetCustomerProductPriceQuery req, CancellationToken ct)
    {
        var price = await _repo.Query()
            .Where(p => p.CustomerId == req.CustomerId && p.ProductId == req.ProductId && p.IsActive)
            .Select(p => (decimal?)p.UnitPrice)
            .FirstOrDefaultAsync(ct);
        return ApiResponse<decimal?>.Ok(price);   // null = no customer-specific price set
    }
}

// ── Create ──
public sealed record CreateCustomerProductPriceCommand(
    int CustomerId, int ProductId, decimal UnitPrice,
    string? BuyerProductCode, string? BuyerProductName, string? Notes)
    : IRequest<ApiResponse<int>>;

public sealed class CreateCustomerProductPriceCommandValidator : AbstractValidator<CreateCustomerProductPriceCommand>
{
    public CreateCustomerProductPriceCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BuyerProductCode).MaximumLength(50);
        RuleFor(x => x.BuyerProductName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class CreateCustomerProductPriceCommandHandler
    : IRequestHandler<CreateCustomerProductPriceCommand, ApiResponse<int>>
{
    private readonly IRepository<CustomerProductPrice> _repo;
    private readonly IUnitOfWork _uow;
    public CreateCustomerProductPriceCommandHandler(IRepository<CustomerProductPrice> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        if (await _repo.Query().AnyAsync(p => p.CustomerId == cmd.CustomerId && p.ProductId == cmd.ProductId, ct))
            return ApiResponse<int>.Fail("This product already has a price for this customer (edit it instead).");

        var e = new CustomerProductPrice
        {
            CustomerId = cmd.CustomerId,
            ProductId = cmd.ProductId,
            UnitPrice = cmd.UnitPrice,
            BuyerProductCode = CustomerPricingText.Clean(cmd.BuyerProductCode),
            BuyerProductName = CustomerPricingText.Clean(cmd.BuyerProductName),
            IsActive = true,
            Notes = CustomerPricingText.Clean(cmd.Notes)
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Customer price added.");
    }
}

// ── Update ──
public sealed record UpdateCustomerProductPriceCommand(
    int Id, decimal UnitPrice, string? BuyerProductCode, string? BuyerProductName, bool IsActive, string? Notes)
    : IRequest<ApiResponse<int>>;

public sealed class UpdateCustomerProductPriceCommandValidator : AbstractValidator<UpdateCustomerProductPriceCommand>
{
    public UpdateCustomerProductPriceCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BuyerProductCode).MaximumLength(50);
        RuleFor(x => x.BuyerProductName).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

internal sealed class UpdateCustomerProductPriceCommandHandler
    : IRequestHandler<UpdateCustomerProductPriceCommand, ApiResponse<int>>
{
    private readonly IRepository<CustomerProductPrice> _repo;
    private readonly IUnitOfWork _uow;
    public UpdateCustomerProductPriceCommandHandler(IRepository<CustomerProductPrice> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<int>.Fail("Customer price not found.");
        e.UnitPrice = cmd.UnitPrice;
        e.BuyerProductCode = CustomerPricingText.Clean(cmd.BuyerProductCode);
        e.BuyerProductName = CustomerPricingText.Clean(cmd.BuyerProductName);
        e.IsActive = cmd.IsActive;
        e.Notes = CustomerPricingText.Clean(cmd.Notes);
        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Customer price updated.");
    }
}

// ── Delete ──
public sealed record DeleteCustomerProductPriceCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteCustomerProductPriceCommandHandler
    : IRequestHandler<DeleteCustomerProductPriceCommand, ApiResponse>
{
    private readonly IRepository<CustomerProductPrice> _repo;
    private readonly IUnitOfWork _uow;
    public DeleteCustomerProductPriceCommandHandler(IRepository<CustomerProductPrice> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteCustomerProductPriceCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Customer price not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Customer price deleted.");
    }
}
