using BengalTex.ERP.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BengalTex.ERP.Domain.Tests.ValueObjects;

public class GeoLocationTests
{
    [Fact]
    public void Create_ValidCoords_ReturnsLocation()
    {
        var loc = GeoLocation.Create(23.8103, 90.4125);
        loc.Latitude.Should().Be(23.8103);
        loc.Longitude.Should().Be(90.4125);
    }

    [Theory]
    [InlineData(91, 0)]
    [InlineData(-91, 0)]
    [InlineData(0, 181)]
    [InlineData(0, -181)]
    public void Create_OutOfRange_Throws(double lat, double lng)
    {
        var act = () => GeoLocation.Create(lat, lng);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void DistanceMetersTo_SamePoint_IsZero()
    {
        var a = GeoLocation.Create(23.8103, 90.4125);
        a.DistanceMetersTo(a).Should().BeApproximately(0, 0.001);
    }

    [Fact]
    public void DistanceMetersTo_KnownPoints_IsAccurate()
    {
        // Dhaka to Chittagong is ~214 km straight-line (Haversine/aerial distance)
        var dhaka = GeoLocation.Create(23.8103, 90.4125);
        var chittagong = GeoLocation.Create(22.3569, 91.7832);
        var distanceKm = dhaka.DistanceMetersTo(chittagong) / 1000;
        distanceKm.Should().BeInRange(210, 220);
    }
}