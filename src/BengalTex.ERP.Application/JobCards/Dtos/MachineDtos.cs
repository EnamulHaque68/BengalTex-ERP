namespace BengalTex.ERP.Application.JobCards.Dtos;

public record MachineDto(
    int Id,
    string Code,
    string Name,
    string? MachineType,
    string? Location,
    decimal? CapacityPerHour,
    string? Notes,
    bool IsActive);
