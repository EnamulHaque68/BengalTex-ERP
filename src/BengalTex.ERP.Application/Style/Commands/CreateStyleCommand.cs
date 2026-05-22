using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Application.Style.Dtos;
using BengalTex.ERP.Application.Style.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Style.Commands;

public sealed record CreateStyleCommand(
    string? Code,
    string StyleName,
    int BuyerId,
    int? ProductId,
    string? BuyerStyleRef,
    string? Season,
    string Status,
    string? Description,
    string? Notes
) : IRequest<ApiResponse<StyleDto>>;

public sealed class CreateStyleCommandValidator : AbstractValidator<CreateStyleCommand>
{
    public CreateStyleCommandValidator()
    {
        RuleFor(x => x.Code).MaximumLength(50)
            .Matches("^[A-Z0-9/_-]+$").When(x => !string.IsNullOrEmpty(x.Code))
            .WithMessage("Code must contain uppercase letters, digits, slash, hyphen, underscore.");
        RuleFor(x => x.StyleName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.BuyerId).GreaterThan(0);
        RuleFor(x => x.BuyerStyleRef).MaximumLength(100);
        RuleFor(x => x.Season).MaximumLength(50);
        RuleFor(x => x.Status).NotEmpty()
            .Must(s => Enum.TryParse<StyleStatus>(s, out _))
            .WithMessage("Status must be Development, Approved, Running, or Discontinued.");
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateStyleCommandHandler
    : IRequestHandler<CreateStyleCommand, ApiResponse<StyleDto>>
{
    private readonly IRepository<Domain.Entities.Style> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateStyleCommandHandler(
        IRepository<Domain.Entities.Style> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        INumberingService numbering,
        IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StyleDto>> Handle(CreateStyleCommand cmd, CancellationToken ct)
    {
        if (await _customerRepo.GetByIdAsync(cmd.BuyerId, ct) is null)
            return ApiResponse<StyleDto>.Fail("Buyer (customer) not found.");
        if (cmd.ProductId.HasValue && await _productRepo.GetByIdAsync(cmd.ProductId.Value, ct) is null)
            return ApiResponse<StyleDto>.Fail("Product not found.");

        var code = string.IsNullOrWhiteSpace(cmd.Code)
            ? await _numbering.NextAsync("STY", null, ct)
            : cmd.Code.Trim().ToUpperInvariant();
        if (await _repo.AnyAsync(s => s.Code == code, ct))
            return ApiResponse<StyleDto>.Fail($"Style code '{code}' already exists.");

        var entity = new Domain.Entities.Style
        {
            Code = code,
            StyleName = cmd.StyleName,
            BuyerId = cmd.BuyerId,
            ProductId = cmd.ProductId,
            BuyerStyleRef = cmd.BuyerStyleRef,
            Season = cmd.Season,
            Status = Enum.Parse<StyleStatus>(cmd.Status),
            Description = cmd.Description,
            Notes = cmd.Notes,
            IsActive = true
        };

        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetStyleByIdQuery(entity.Id), ct);
    }
}
