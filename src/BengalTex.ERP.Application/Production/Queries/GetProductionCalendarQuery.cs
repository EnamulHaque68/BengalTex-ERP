using BengalTex.ERP.Application.Production.Dtos;
using BengalTex.ERP.Domain.Common;
using BengalTex.ERP.Shared.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BengalTex.ERP.Application.Production.Queries;

/// <summary>
/// Manufacturing Calendar — production orders whose planned/actual span overlaps [From, To], plus
/// the holiday + weekly-off context for shading. Read-only; reuses ProductionOrder, Holiday and the
/// Shift weekend config. The range is capped to keep the payload bounded.
/// </summary>
public sealed record GetProductionCalendarQuery(DateOnly From, DateOnly To)
    : IRequest<ApiResponse<ProductionCalendarDto>>;

internal sealed class GetProductionCalendarQueryHandler
    : IRequestHandler<GetProductionCalendarQuery, ApiResponse<ProductionCalendarDto>>
{
    private const int MaxRangeDays = 120;   // ~4 months — comfortably covers a 6-week month grid

    private readonly IRepository<Domain.Entities.ProductionOrder, long> _orders;
    private readonly IRepository<Domain.Entities.Holiday> _holidays;
    private readonly IRepository<Domain.Entities.Shift> _shifts;

    public GetProductionCalendarQueryHandler(
        IRepository<Domain.Entities.ProductionOrder, long> orders,
        IRepository<Domain.Entities.Holiday> holidays,
        IRepository<Domain.Entities.Shift> shifts)
    {
        _orders = orders;
        _holidays = holidays;
        _shifts = shifts;
    }

    public async Task<ApiResponse<ProductionCalendarDto>> Handle(
        GetProductionCalendarQuery request, CancellationToken ct)
    {
        var from = request.From;
        var to = request.To;
        if (to < from) return ApiResponse<ProductionCalendarDto>.Fail("'to' date must be on or after 'from'.");
        if (to.DayNumber - from.DayNumber > MaxRangeDays)
            return ApiResponse<ProductionCalendarDto>.Fail($"Date range too large (max {MaxRangeDays} days).");

        // Orders whose planned span — or, falling back, actual span — overlaps the window.
        var orders = await _orders.Query().AsNoTracking()
            .Where(p => p.Status != Domain.Entities.ProductionOrderStatus.Cancelled)
            .Where(p =>
                (p.PlannedStartDate != null
                    && p.PlannedStartDate <= to
                    && (p.PlannedEndDate ?? p.PlannedStartDate) >= from)
                ||
                (p.ActualStartDate != null
                    && p.ActualStartDate <= to
                    && (p.ActualEndDate ?? p.ActualStartDate) >= from))
            .OrderBy(p => p.PlannedStartDate ?? p.ActualStartDate)
            .Select(p => new ProductionCalendarEventDto(
                p.Id, p.Code,
                p.ProductId, p.Product.Name,
                p.Quantity,
                p.Status.ToString(),
                p.PlannedStartDate, p.PlannedEndDate,
                p.ActualStartDate, p.ActualEndDate,
                p.SalesOrderId,
                p.SalesOrder != null ? p.SalesOrder.Code : null))
            .ToListAsync(ct);

        var holidays = await _holidays.Query().AsNoTracking()
            .Where(h => h.IsActive && h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .Select(h => new ProductionCalendarHolidayDto(h.Date, h.Name))
            .ToListAsync(ct);

        // Weekly off-days come from the factory's shift config (default Friday when none is set).
        var shift = await _shifts.Query().AsNoTracking()
            .OrderBy(s => s.Id)
            .Select(s => new { s.WeekendDayOfWeek, s.SecondWeekendDayOfWeek })
            .FirstOrDefaultAsync(ct);

        var weekendDays = new List<int>();
        if (shift is null)
        {
            weekendDays.Add((int)DayOfWeek.Friday);
        }
        else
        {
            weekendDays.Add((int)shift.WeekendDayOfWeek);
            if (shift.SecondWeekendDayOfWeek.HasValue)
                weekendDays.Add((int)shift.SecondWeekendDayOfWeek.Value);
        }

        var dto = new ProductionCalendarDto(from, to, weekendDays, holidays, orders);
        return ApiResponse<ProductionCalendarDto>.Ok(dto);
    }
}
