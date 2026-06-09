namespace BengalTex.ERP.Application.Reports.Dtos;

/// <summary>
/// Per-active-SO row inside a buyer's order book. "Active" = SO not Cancelled and not
/// fully Closed (Confirmed / PartiallyDispatched / Dispatched / Delivered all count).
/// Each row tells the sales team where this specific order stands: order value, what's
/// shipped, what's still owed.
/// </summary>
public record BuyerOrderBookSalesOrderDto(
    long SalesOrderId,
    string Code,
    DateOnly OrderDate,
    DateOnly? RequiredDeliveryDate,
    string? CustomerPoRef,
    string Status,
    string CurrencyCode,
    decimal ExchangeRate,
    decimal TotalAmount,                  // in SO currency
    decimal BaseTotalAmount,              // = Total × ExchangeRate (BDT)
    decimal OrderedQuantity,              // Σ lines
    decimal DispatchedQuantity,           // Σ lines
    decimal PendingQuantity,              // Ordered − Dispatched
    decimal CompletionPercent,            // Dispatched / Ordered × 100
    bool IsOverdue);                      // RequiredDeliveryDate < today AND not fully dispatched

/// <summary>
/// Buyer-level rollup. Sales managers use this view to see "where is each buyer at" —
/// total order book value, what's already shipped, what's still in the pipeline,
/// and what's overdue.
/// </summary>
public record BuyerOrderBookRowDto(
    int CustomerId,
    string CustomerCode,
    string CustomerName,
    int? CreditPeriodDays,
    decimal? CreditLimit,                 // in BDT
    int ActiveOrderCount,
    int OverdueOrderCount,
    decimal TotalOrderValueBdt,           // Σ active SOs' BaseTotalAmount
    decimal DispatchedValueBdt,           // est. = total × dispatchedQty/orderedQty per SO, summed
    decimal PendingValueBdt,              // total − dispatched
    decimal OutstandingInvoiceBdt,        // sum of (TotalAmount − AmountPaid) × ExchangeRate on non-Draft/non-Cancelled invoices
    IReadOnlyList<BuyerOrderBookSalesOrderDto> Orders);

public record BuyerOrderBookReportDto(
    DateOnly AsOfDate,
    int? CustomerIdFilter,                // null = all customers
    int TotalBuyersWithActiveOrders,
    int TotalActiveOrders,
    int TotalOverdueOrders,
    decimal GrandTotalOrderValueBdt,
    decimal GrandDispatchedValueBdt,
    decimal GrandPendingValueBdt,
    decimal GrandOutstandingInvoiceBdt,
    IReadOnlyList<BuyerOrderBookRowDto> Rows);
