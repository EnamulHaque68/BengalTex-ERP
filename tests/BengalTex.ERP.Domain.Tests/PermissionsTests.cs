using BengalTex.ERP.Shared.Permissions;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Domain.Tests;

public class PermissionsTests
{
    [Fact]
    public void GetAll_ReturnsAllConstants()
    {
        var all = Permissions.GetAll();
        all.Should().NotBeEmpty();
        all.Should().Contain(Permissions.Customers.View);
        all.Should().Contain(Permissions.SalesOrders.Confirm);
        all.Should().Contain(Permissions.Attendance.ApproveFlagged);
    }

    [Fact]
    public void GetAll_HasNoDuplicates()
    {
        var all = Permissions.GetAll();
        all.Distinct().Should().HaveCount(all.Count);
    }

    [Fact]
    public void GetAll_AllFollowResourceDotActionFormat()
    {
        var all = Permissions.GetAll();
        foreach (var p in all)
        {
            p.Should().Contain(".");
            p.Split('.').Should().HaveCount(2);
        }
    }
}