namespace BengalTex.ERP.Domain.ValueObjects;

/// <summary>
/// GPS coordinate value object. WGS84 lat/long. Pure C# — no spatial library dependency.
/// Conversion to/from spatial types happens in Infrastructure layer.
/// </summary>
public sealed record GeoLocation
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    private GeoLocation(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    public static GeoLocation Create(double latitude, double longitude)
    {
        if (latitude < -90 || latitude > 90)
            throw new ArgumentOutOfRangeException(nameof(latitude), "Latitude must be between -90 and 90.");
        if (longitude < -180 || longitude > 180)
            throw new ArgumentOutOfRangeException(nameof(longitude), "Longitude must be between -180 and 180.");
        return new GeoLocation(latitude, longitude);
    }

    /// <summary>
    /// Approximate distance in meters using Haversine formula.
    /// In-memory only — for DB queries use SQL STDistance via Infrastructure.
    /// </summary>
    public double DistanceMetersTo(GeoLocation other)
    {
        const double earthRadiusMeters = 6_371_000;
        var lat1 = ToRadians(Latitude);
        var lat2 = ToRadians(other.Latitude);
        var dLat = ToRadians(other.Latitude - Latitude);
        var dLon = ToRadians(other.Longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}