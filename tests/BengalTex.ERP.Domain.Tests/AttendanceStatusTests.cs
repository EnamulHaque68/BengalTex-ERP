using BengalTex.ERP.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Domain.Tests;

/// <summary>
/// Guards the shared present-day classification that payroll, dashboards and "My Attendance"
/// all rely on. The Attendance upgrade introduced several new "present-like" statuses
/// (OnTime, EarlyLeave, OffdayWork, HolidayWork, Overtime) — these MUST still count as a full
/// present working day, otherwise payroll silently under-pays self-checked-in employees.
/// </summary>
public class AttendanceStatusTests
{
    [Theory]
    [InlineData(AttendanceStatus.Present)]
    [InlineData(AttendanceStatus.Late)]
    [InlineData(AttendanceStatus.OnTime)]
    [InlineData(AttendanceStatus.EarlyLeave)]
    [InlineData(AttendanceStatus.OffdayWork)]
    [InlineData(AttendanceStatus.HolidayWork)]
    [InlineData(AttendanceStatus.Overtime)]
    public void CountsAsFullPresent_PresentLikeStatuses_AreTrue(AttendanceStatus status)
        => status.CountsAsFullPresent().Should().BeTrue();

    [Theory]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Leave)]
    [InlineData(AttendanceStatus.Holiday)]
    [InlineData(AttendanceStatus.HalfDay)]
    [InlineData(AttendanceStatus.ManualAdjustment)]
    public void CountsAsFullPresent_NonPresentStatuses_AreFalse(AttendanceStatus status)
        => status.CountsAsFullPresent().Should().BeFalse();

    [Fact]
    public void CountsAsPresent_IncludesHalfDay()
    {
        AttendanceStatus.HalfDay.CountsAsPresent().Should().BeTrue();
        AttendanceStatus.HalfDay.CountsAsFullPresent().Should().BeFalse();
    }

    [Theory]
    [InlineData(AttendanceStatus.Absent)]
    [InlineData(AttendanceStatus.Leave)]
    [InlineData(AttendanceStatus.Holiday)]
    public void CountsAsPresent_AbsentLikeStatuses_AreFalse(AttendanceStatus status)
        => status.CountsAsPresent().Should().BeFalse();
}
