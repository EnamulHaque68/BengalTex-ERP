using BengalTex.ERP.Application.Samples.Dtos;
using BengalTex.ERP.Application.Samples.Queries;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Domain.Entities;
using BengalTex.ERP.Shared.Common;
using FluentValidation;
using MediatR;

namespace BengalTex.ERP.Application.Samples.Commands;

public sealed record UpdateSampleCommand(
    long Id,
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

public sealed class UpdateSampleCommandValidator : AbstractValidator<UpdateSampleCommand>
{
    public UpdateSampleCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.BuyerReference).MaximumLength(100);
        RuleFor(x => x.Quantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RequestedDate).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

internal sealed class UpdateSampleCommandHandler : IRequestHandler<UpdateSampleCommand, ApiResponse<SampleDto>>
{
    private readonly IRepository<Domain.Entities.Sample, long> _repo;
    private readonly IUnitOfWork _uow;
    private readonly IMediator _mediator;

    public UpdateSampleCommandHandler(IRepository<Domain.Entities.Sample, long> repo, IUnitOfWork uow, IMediator mediator)
    {
        _repo = repo;
        _uow = uow;
        _mediator = mediator;
    }

    public async Task<ApiResponse<SampleDto>> Handle(UpdateSampleCommand cmd, CancellationToken ct)
    {
        var s = await _repo.GetByIdAsync(cmd.Id, ct);
        if (s is null) return ApiResponse<SampleDto>.Fail("Sample not found.");
        if (s.Status is SampleStatus.Approved or SampleStatus.Rejected)
            return ApiResponse<SampleDto>.Fail("A decided sample can no longer be edited.");

        s.ProductId = cmd.ProductId;
        s.StyleId = cmd.StyleId;
        s.Title = cmd.Title.Trim();
        s.Description = string.IsNullOrWhiteSpace(cmd.Description) ? null : cmd.Description.Trim();
        s.BuyerReference = string.IsNullOrWhiteSpace(cmd.BuyerReference) ? null : cmd.BuyerReference.Trim();
        s.Quantity = cmd.Quantity;
        s.RequestedDate = cmd.RequestedDate;
        s.TargetDate = cmd.TargetDate;
        s.Notes = string.IsNullOrWhiteSpace(cmd.Notes) ? null : cmd.Notes.Trim();
        _repo.Update(s);
        await _uow.SaveChangesAsync(ct);
        return await _mediator.Send(new GetSampleByIdQuery(s.Id), ct);
    }
}
