using System.Globalization;

namespace FSH.BlazorShared.Formatting;

/// <summary>
/// Deterministic formatting helpers for money/dates/numbers (React parity:
/// Intl.NumberFormat currency + Intl.DateTimeFormat short/long dates).
/// Repo convention: "USD 0.00" (matches TenantCreateDialogTests).
/// </summary>
public static class FshFormat
{
    public static string Money(decimal amount, string currency) =>
        $"{currency} {amount:N2}";

    /// <summary>Short date like "Jul 02, 2026" (React dateShort).</summary>
    public static string DateShort(DateTime? utc) =>
        utc is null ? "—" : utc.Value.ToString("MMM dd, yyyy", CultureInfo.InvariantCulture);

    /// <summary>Short date+time like "Jul 02, 2026 14:22" (React dateTimeShort).</summary>
    public static string DateTimeShort(DateTime? utc) =>
        utc is null ? "—" : utc.Value.ToString("MMM dd, yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>Long date like "July 2, 2026" (React dateLong).</summary>
    public static string DateLong(DateTime? utc) =>
        utc is null ? "—" : utc.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>Billing period like "2026-07".</summary>
    public static string Period(int year, int month) => $"{year}-{month:00}";

    public static string Number(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);

    /// <summary>Local time like "04-30 14:22:01" (React audit formatTimestamp).</summary>
    public static string Timestamp(DateTime? utc)
    {
        if (utc is null)
        {
            return "—";
        }

        var local = utc.Value.ToLocalTime();
        return $"{local:MM-dd} {local:HH:mm:ss}";
    }

    /// <summary>ISO-8601 round-trip like "2026-04-30T14:22:01.0000000Z" (React detail sheet).</summary>
    public static string TimestampIso(DateTime? utc) =>
        utc is null ? "-" : utc.Value.ToString("O", CultureInfo.InvariantCulture);

    /// <summary>Local long form like "7/2/2026 9:00:01 AM" (React toLocaleString for notifications).</summary>
    public static string DateTimeLong(DateTime? utc)
    {
        if (utc is null)
        {
            return "-";
        }

        return utc.Value.ToLocalTime().ToString("g", CultureInfo.CurrentCulture);
    }
}
