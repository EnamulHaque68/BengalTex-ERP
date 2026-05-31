namespace BengalTex.ERP.Application.Compliance;

/// <summary>
/// Single source of truth for the expiry-status classification used across
/// certificate queries + dashboard.
/// </summary>
public static class ExpiryStatus
{
    /// <summary>Threshold (days) under which a certificate is "Expiring Soon".</summary>
    public const int ExpiringSoonDays = 60;

    public const string Active = "Active";
    public const string ExpiringSoonStatus = "ExpiringSoon";
    public const string Expired = "Expired";

    public static string ClassifyDays(int daysUntilExpiry) =>
        daysUntilExpiry < 0 ? Expired
        : daysUntilExpiry <= ExpiringSoonDays ? ExpiringSoonStatus
        : Active;
}
