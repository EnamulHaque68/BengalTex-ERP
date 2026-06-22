namespace BengalTex.ERP.Application.Attendance.Dtos;

/// <summary>An office location with its geo-fence + how many employees are authorized there.</summary>
public sealed record OfficeLocationDto(
    int Id,
    string Name,
    string Type,                   // HeadOffice | Factory | Warehouse | BranchOffice
    double Latitude,
    double Longitude,
    double RadiusMeters,
    string? Address,
    bool IsActive,
    int AssignedEmployeeCount);

/// <summary>One employee's assignment state for a location (used by the assignment picker).</summary>
public sealed record OfficeLocationEmployeeDto(
    int EmployeeId,
    string EmployeeCode,
    string EmployeeName,
    string? Designation,
    string? Department,
    bool Assigned);
