using BengalTex.ERP.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Commands;

/// <summary>
/// Shared validation for linking a production order to a source Sales Order line
/// (Phase 1 — Sales-driven production). Reused by Create + Update so the rules stay identical.
///
/// Rules enforced:
///   • Both <c>SalesOrderId</c> and <c>SalesOrderLineId</c> are supplied together (or both null = standalone).
///   • The sales order exists and is confirmed (not Draft / PendingApproval / Cancelled).
///   • The line belongs to that sales order.
///   • The production order's product matches the line's product.
///   • The new quantity does not exceed the line's remaining (un-allocated) quantity —
///     Remaining = Ordered − Σ(quantity of all OTHER non-cancelled production orders on the line).
/// Returns an error message, or <c>null</c> when valid (including the standalone case).
/// </summary>
internal static class ProductionSalesLink
{
    public static async Task<string?> ValidateAsync(
        IRepository<Domain.Entities.SalesOrder, long> soRepo,
        IRepository<Domain.Entities.ProductionOrder, long> poRepo,
        long? salesOrderId,
        long? salesOrderLineId,
        int productId,
        decimal quantity,
        long? excludeProductionOrderId,
        CancellationToken ct)
    {
        // Standalone run — nothing to validate (existing behaviour, unchanged).
        if (salesOrderId is null && salesOrderLineId is null) return null;

        if (salesOrderId is null || salesOrderLineId is null)
            return "Select both a source sales order and the order line to fulfil.";

        var so = await soRepo.Query()
            .AsNoTracking()
            .Include(s => s.Lines)
            .FirstOrDefaultAsync(s => s.Id == salesOrderId.Value, ct);

        if (so is null) return "Source sales order not found.";

        if (so.Status is Domain.Entities.SalesOrderStatus.Draft
            or Domain.Entities.SalesOrderStatus.PendingApproval
            or Domain.Entities.SalesOrderStatus.Cancelled)
            return $"Sales order {so.Code} must be confirmed before production can be planned against it.";

        var line = so.Lines.FirstOrDefault(l => l.Id == salesOrderLineId.Value);
        if (line is null) return "The selected order line does not belong to the source sales order.";

        if (line.ProductId != productId)
            return "The production product must match the selected sales order line's product.";

        // Remaining = ordered − already-allocated (all other non-cancelled production orders on this line).
        var allocated = await poRepo.Query()
            .AsNoTracking()
            .Where(p => p.SalesOrderLineId == salesOrderLineId.Value
                && p.Status != Domain.Entities.ProductionOrderStatus.Cancelled
                && (excludeProductionOrderId == null || p.Id != excludeProductionOrderId.Value))
            .SumAsync(p => (decimal?)p.Quantity, ct) ?? 0m;

        var remaining = line.Quantity - allocated;
        if (quantity > remaining)
            return $"Quantity exceeds the remaining quantity for {line.Product?.Name ?? "this line"} "
                 + $"(ordered {line.Quantity:0.####}, already planned {allocated:0.####}, remaining {remaining:0.####}).";

        return null;
    }
}
