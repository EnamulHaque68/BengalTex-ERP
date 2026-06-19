using BengalTex.ERP.Application.Common.Interfaces;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.ProductVariants;

internal static class ProductVariantText
{
    public static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}

// ── Create ──
public sealed record CreateProductVariantCommand(
    int ProductId, string VariantCode, string? Name, string? Color, string? Size,
    string? Sku, decimal? SalesPriceOverride, string? Notes, bool IsActive)
    : IRequest<ApiResponse<int>>;

public sealed class CreateProductVariantCommandValidator : AbstractValidator<CreateProductVariantCommand>
{
    public CreateProductVariantCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.VariantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Size).MaximumLength(50);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.SalesPriceOverride).GreaterThanOrEqualTo(0).When(x => x.SalesPriceOverride.HasValue);
    }
}

internal sealed class CreateProductVariantCommandHandler : IRequestHandler<CreateProductVariantCommand, ApiResponse<int>>
{
    private readonly IRepository<Domain.Entities.ProductVariant> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;

    public CreateProductVariantCommandHandler(
        IRepository<Domain.Entities.ProductVariant> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow)
    { _repo = repo; _productRepo = productRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(CreateProductVariantCommand cmd, CancellationToken ct)
    {
        var product = await _productRepo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return ApiResponse<int>.Fail("Product not found.");

        var code = cmd.VariantCode.Trim();
        var exists = await _repo.Query().AnyAsync(v => v.ProductId == cmd.ProductId && v.VariantCode == code, ct);
        if (exists) return ApiResponse<int>.Fail($"Variant code '{code}' already exists for this product.");

        var e = new Domain.Entities.ProductVariant
        {
            ProductId = cmd.ProductId,
            VariantCode = code,
            Name = ProductVariantText.Clean(cmd.Name),
            Color = ProductVariantText.Clean(cmd.Color),
            Size = ProductVariantText.Clean(cmd.Size),
            Sku = ProductVariantText.Clean(cmd.Sku),
            SalesPriceOverride = cmd.SalesPriceOverride,
            Notes = ProductVariantText.Clean(cmd.Notes),
            IsActive = cmd.IsActive
        };
        await _repo.AddAsync(e, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Variant created.");
    }
}

// ── Update ──
public sealed record UpdateProductVariantCommand(
    int Id, string VariantCode, string? Name, string? Color, string? Size,
    string? Sku, decimal? SalesPriceOverride, string? Notes, bool IsActive)
    : IRequest<ApiResponse<int>>;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.VariantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Name).MaximumLength(200);
        RuleFor(x => x.Color).MaximumLength(50);
        RuleFor(x => x.Size).MaximumLength(50);
        RuleFor(x => x.Sku).MaximumLength(100);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.SalesPriceOverride).GreaterThanOrEqualTo(0).When(x => x.SalesPriceOverride.HasValue);
    }
}

internal sealed class UpdateProductVariantCommandHandler : IRequestHandler<UpdateProductVariantCommand, ApiResponse<int>>
{
    private readonly IRepository<Domain.Entities.ProductVariant> _repo;
    private readonly IUnitOfWork _uow;

    public UpdateProductVariantCommandHandler(IRepository<Domain.Entities.ProductVariant> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(UpdateProductVariantCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse<int>.Fail("Variant not found.");

        var code = cmd.VariantCode.Trim();
        var clash = await _repo.Query()
            .AnyAsync(v => v.ProductId == e.ProductId && v.VariantCode == code && v.Id != e.Id, ct);
        if (clash) return ApiResponse<int>.Fail($"Variant code '{code}' already exists for this product.");

        e.VariantCode = code;
        e.Name = ProductVariantText.Clean(cmd.Name);
        e.Color = ProductVariantText.Clean(cmd.Color);
        e.Size = ProductVariantText.Clean(cmd.Size);
        e.Sku = ProductVariantText.Clean(cmd.Sku);
        e.SalesPriceOverride = cmd.SalesPriceOverride;
        e.Notes = ProductVariantText.Clean(cmd.Notes);
        e.IsActive = cmd.IsActive;

        _repo.Update(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(e.Id, "Variant updated.");
    }
}

// ── Bulk generate (color × size matrix) ──
public sealed record BulkCreateProductVariantsCommand(
    int ProductId, IReadOnlyList<string> Colors, IReadOnlyList<string> Sizes, string? SkuPrefix)
    : IRequest<ApiResponse<int>>;

public sealed class BulkCreateProductVariantsCommandValidator : AbstractValidator<BulkCreateProductVariantsCommand>
{
    public BulkCreateProductVariantsCommandValidator()
    {
        RuleFor(x => x.ProductId).GreaterThan(0);
        RuleFor(x => x.SkuPrefix).MaximumLength(50);
        RuleFor(x => x)
            .Must(x => (x.Colors?.Any(c => !string.IsNullOrWhiteSpace(c)) ?? false)
                    || (x.Sizes?.Any(s => !string.IsNullOrWhiteSpace(s)) ?? false))
            .WithMessage("Provide at least one colour or size to generate variants.");
    }
}

internal sealed class BulkCreateProductVariantsCommandHandler
    : IRequestHandler<BulkCreateProductVariantsCommand, ApiResponse<int>>
{
    private readonly IRepository<Domain.Entities.ProductVariant> _repo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;

    public BulkCreateProductVariantsCommandHandler(
        IRepository<Domain.Entities.ProductVariant> repo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow)
    { _repo = repo; _productRepo = productRepo; _uow = uow; }

    public async Task<ApiResponse<int>> Handle(BulkCreateProductVariantsCommand cmd, CancellationToken ct)
    {
        var product = await _productRepo.GetByIdAsync(cmd.ProductId, ct);
        if (product is null) return ApiResponse<int>.Fail("Product not found.");

        // Clean & de-dupe each dimension; an empty dimension contributes a single "null" slot
        // so a colour-only or size-only matrix still generates one row per value.
        var colors = Clean(cmd.Colors);
        var sizes = Clean(cmd.Sizes);
        if (colors.Count == 0) colors.Add(null);
        if (sizes.Count == 0) sizes.Add(null);

        var existing = (await _repo.Query()
                .Where(v => v.ProductId == cmd.ProductId)
                .Select(v => v.VariantCode)
                .ToListAsync(ct))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var prefix = string.IsNullOrWhiteSpace(cmd.SkuPrefix) ? null : cmd.SkuPrefix.Trim();
        var toAdd = new List<Domain.Entities.ProductVariant>();
        foreach (var color in colors)
        {
            foreach (var size in sizes)
            {
                if (color is null && size is null) continue;
                var code = CodeOf(color, size);
                if (string.IsNullOrEmpty(code) || !existing.Add(code)) continue;   // skip dupes (existing + within-batch)

                toAdd.Add(new Domain.Entities.ProductVariant
                {
                    ProductId = cmd.ProductId,
                    VariantCode = code,
                    Name = NameOf(color, size),
                    Color = color,
                    Size = size,
                    Sku = prefix is null ? null : $"{prefix}-{code}",
                    IsActive = true
                });
            }
        }

        if (toAdd.Count == 0)
            return ApiResponse<int>.Fail("No new variants to create — all combinations already exist.");

        foreach (var v in toAdd) await _repo.AddAsync(v, ct);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse<int>.Ok(toAdd.Count, $"{toAdd.Count} variant(s) generated.");
    }

    private static List<string?> Clean(IReadOnlyList<string>? values) =>
        (values ?? Array.Empty<string>())
            .Select(v => v?.Trim())
            .Where(v => !string.IsNullOrEmpty(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<string?>()
            .ToList();

    private static string CodeOf(string? color, string? size)
    {
        var parts = new[] { color, size }.Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!.Trim().ToUpperInvariant().Replace(' ', '-'));
        var code = string.Join("-", parts);
        return code.Length > 50 ? code[..50] : code;
    }

    private static string? NameOf(string? color, string? size)
    {
        var parts = new[] { color, size }.Where(p => !string.IsNullOrWhiteSpace(p));
        var name = string.Join(" / ", parts);
        return string.IsNullOrEmpty(name) ? null : name;
    }
}

// ── Delete ──
public sealed record DeleteProductVariantCommand(int Id) : IRequest<ApiResponse>;

internal sealed class DeleteProductVariantCommandHandler : IRequestHandler<DeleteProductVariantCommand, ApiResponse>
{
    private readonly IRepository<Domain.Entities.ProductVariant> _repo;
    private readonly IUnitOfWork _uow;

    public DeleteProductVariantCommandHandler(IRepository<Domain.Entities.ProductVariant> repo, IUnitOfWork uow)
    { _repo = repo; _uow = uow; }

    public async Task<ApiResponse> Handle(DeleteProductVariantCommand cmd, CancellationToken ct)
    {
        var e = await _repo.GetByIdAsync(cmd.Id, ct);
        if (e is null) return ApiResponse.Fail("Variant not found.");
        _repo.Remove(e);
        await _uow.SaveChangesAsync(ct);
        return ApiResponse.Ok("Variant deleted.");
    }
}
