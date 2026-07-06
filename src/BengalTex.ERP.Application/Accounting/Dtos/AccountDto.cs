namespace BengalTex.ERP.Application.Accounting.Dtos;

public record AccountDto(
    int Id,
    string Code,
    string Name,
    string AccountType,           // Asset | Liability | Equity | Income | Expense
    string NormalBalance,         // Debit | Credit (derived from AccountType)
    bool IsGroup,
    int? ParentAccountId,
    string? ParentAccountName,
    bool IsSystem,
    bool IsActive,
    string? Description,
    bool RequiresCostCenter = false);   // Phase A3 — postings to this account must carry a cost center
