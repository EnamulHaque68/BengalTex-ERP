using BengalTex.ERP.Application.Style.Dtos;
using BengalTex.ERP.Application.Style.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Style.Commands;

public sealed record UpdateStyleCommand(
    int Id,
    string StyleName,
    int BuyerId,
    int? ProductId,
    string? BuyerStyleRef,
    string? Season,
    string Status,
    string? Description,
    string? Notes,
    bool IsActive
) : IRequest<ApiResponse<StyleDto>>;

public sealed class UpdateStyleCommandValidator : AbstractValidator<UpdateStyleCommand>
{
    public UpdateStyleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
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

internal sealed class UpdateStyleCommandHandler
    : IRequestHandler<UpdateStyleCommand, ApiResponse<StyleDto>>
{
    private readonly IRepository<Domain.Entities.Style> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IRepository<Domain.Entities.Product> _productRepo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateStyleCommandHandler(
        IRepository<Domain.Entities.Style> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IRepository<Domain.Entities.Product> productRepo,
        IUnitOfWork uow,
        IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _productRepo = productRepo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<StyleDto>> Handle(UpdateStyleCommand cmd, CancellationToken ct)
    {
        var entity = await _repo.GetByIdAsync(cmd.Id, ct);
        if (entity is null) return ApiResponse<StyleDto>.Fail("Style not found.");
        if (await _customerRepo.GetByIdAsync(cmd.BuyerId, ct) is null)
            return ApiResponse<StyleDto>.Fail("Buyer (customer) not found.");
        if (cmd.ProductId.HasValue && await _productRepo.GetByIdAsync(cmd.ProductId.Value, ct) is null)
            return ApiResponse<StyleDto>.Fail("Product not found.");

        entity.StyleName = cmd.StyleName;
        entity.BuyerId = cmd.BuyerId;
        entity.ProductId = cmd.ProductId;
        entity.BuyerStyleRef = cmd.BuyerStyleRef;
        entity.Season = cmd.Season;
        entity.Status = Enum.Parse<StyleStatus>(cmd.Status);
        entity.Description = cmd.Description;
        entity.Notes = cmd.Notes;
        entity.IsActive = cmd.IsActive;

        _repo.Update(entity);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetStyleByIdQuery(entity.Id), ct);
    }
}
