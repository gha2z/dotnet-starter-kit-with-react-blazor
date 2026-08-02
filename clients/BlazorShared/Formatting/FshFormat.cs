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

    /// <summary>Long date like "July 2, 2026" (React dateLong).</summary>
    public static string DateLong(DateTime? utc) =>
        utc is null ? "—" : utc.Value.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>Billing period like "2026-07".</summary>
    public static string Period(int year, int month) => $"{year}-{month:00}";

    public static string Number(decimal value) => value.ToString("N0", CultureInfo.InvariantCulture);
}
