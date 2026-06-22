using BengalTex.ERP.Application.Employee.Dtos;
using Mapster;

namespace BengalTex.ERP.Application.Employee;

public class EmployeeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Flatten the supervisor's name from the ReportingTo navigation (null-safe).
        config.NewConfig<Domain.Entities.Employee, EmployeeDto>()
            .Map(d => d.ReportingToName, s => s.ReportingTo != null ? s.ReportingTo.FullName : null);

        config.NewConfig<Domain.Entities.Employee, EmployeeListItemDto>()
            .Map(d => d.ReportingToName, s => s.ReportingTo != null ? s.ReportingTo.FullName : null);
    }
}
