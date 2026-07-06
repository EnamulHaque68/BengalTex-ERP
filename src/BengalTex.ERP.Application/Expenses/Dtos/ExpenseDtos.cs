namespace BengalTex.ERP.Application.Expenses.Dtos;

public record ExpenseCategoryDto(
    int Id,
    string Name,
    int? LedgerAccountId,
    string? LedgerAccountCode,
    string? LedgerAccountName,
    bool IsActive,
    string? Description);

public record ExpenseDto(
    long Id,
    string Code,
    DateOnly ExpenseDate,
    int ExpenseCategoryId,
    string ExpenseCategoryName,
    decimal Amount,
    string PaymentMethod,
    string? Payee,
    string? ReferenceNumber,
    string? Description,
    string Status,
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    int? CostCenterId = null,
    string? CostCenterName = null);   // Phase A3

public record ExpenseListItemDto(
    long Id,
    string Code,
    DateOnly ExpenseDate,
    string ExpenseCategoryName,
    decimal Amount,
    string PaymentMethod,
    string? Payee,
    string Status);

public record ExpenseSummaryRowDto(
    int ExpenseCategoryId,
    string ExpenseCategoryName,
    decimal Amount,
    int Count);

public record ExpenseSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    IReadOnlyList<ExpenseSummaryRowDto> Rows,
    decimal TotalAmount);
