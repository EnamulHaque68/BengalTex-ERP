using BengalTex.ERP.Application.Employee.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MapsterMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Employee.Queries;

public sealed record GetEmployeeByIdQuery(int Id) : IRequest<ApiResponse<EmployeeDto>>;

internal sealed class GetEmployeeByIdQueryHandler
    : IRequestHandler<GetEmployeeByIdQuery, ApiResponse<EmployeeDto>>
{
    private readonly IRepository<Domain.Entities.Employee> _repo;
    private readonly IMapper _mapper;

    public GetEmployeeByIdQueryHandler(IRepository<Domain.Entities.Employee> repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<ApiResponse<EmployeeDto>> Handle(
        GetEmployeeByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await _repo.Query()
            .Include(e => e.ReportingTo)
            .FirstOrDefaultAsync(e => e.Id == request.Id, cancellationToken);
        return entity is null
            ? ApiResponse<EmployeeDto>.Fail("Employee not found.")
            : ApiResponse<EmployeeDto>.Ok(_mapper.Map<EmployeeDto>(entity));
    }
}
