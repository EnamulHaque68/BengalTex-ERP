using BengalTex.ERP.Application.Samples.Dtos;
using BengalTex.ERP.Application.Samples.Queries;
using BengalTex.ERP.Application.Services;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Samples.Commands;

public sealed record CreateSampleCommand(
    int CustomerId,
    int? ProductId,
    int? StyleId,
    string Title,
    string? Description,
    string? BuyerReference,
    decimal Quantity,
    DateOnly RequestedDate,
    DateOnly? TargetDate,
    string? Notes
) : IRequest<ApiResponse<SampleDto>>;

public sealed class CreateSampleCommandValidator : AbstractValidator<CreateSampleCommand>
{
    public CreateSampleCommandValidator()
    {
        RuleFor(x => x.CustomerId).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BuyerReference).MaximumLength(100);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RequestedDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class CreateSampleCommandHandler : IRequestHandler<CreateSampleCommand, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IRepository<Domain.Entities.Customer> _customerRepo;
    private readonly IUnitOfWork _uow;
    private readonly INumberingService _numbering;
    private readonly IMediator _mediator;

    public CreateSampleCommandHandler(
        IRepository<Domain.Entities.Sample, long> repo,
        IRepository<Domain.Entities.Customer> customerRepo,
        IUnitOfWork uow, INumberingService numbering, IMediator mediator)
    {
        _repo = repo;
        _customerRepo = customerRepo;
        _uow = uow;
        _numbering = numbering;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SampleDto>> Handle(CreateSampleCommand cmd, CancellationToken ct)
    {
        if (await _customerRepo.GetByIdAsync(cmd.CustomerId, ct) is null)
            return ApiResponse<SampleDto>.Fail("Customer not found.");

        var entity = new Domain.Entities.Sample
        {
            Code = await _numbering.NextAsync("SMP", null, ct),
            CustomerId = cmd.CustomerId,
            ProductId = cmd.ProductId,
            StyleId = cmd.StyleId,
            Title = cmd.Title.Trim(),
            Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim(),
            BuyerReference = string.IsNullOrWhiteSpace(cmd.BuyerReference) ? null : cmd.BuyerReference.Trim(),
            Quantity = cmd.Quantity,
            RequestedDate = cmd.RequestedDate,
            TargetDate = cmd.TargetDate,
            Status = SampleStatus.Requested,
            Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim()
        };
        await _repo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSampleByIdQuery(entity.Id), ct);
    }
}
