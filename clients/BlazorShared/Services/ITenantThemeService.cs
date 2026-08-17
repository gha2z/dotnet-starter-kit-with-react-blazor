using FSH.BlazorShared.Models.Tenants;

namespace FSH.BlazorShared.Services;

public interface ITenantThemeService
{
    /// <summary>Fetch a tenant's theme. Caller needs MultitenancyPermissions.Tenants.ViewTheme.</summary>
    Task<TenantThemeDto> GetThemeAsync(string tenantId, CancellationToken ct = default);

    /// <summary>Save a tenant's theme. Caller needs MultitenancyPermissions.Tenants.UpdateTheme.</summary>
    Task UpdateThemeAsync(string tenantId, TenantThemeDto theme, CancellationToken ct = default);

    /// <summary>Reset a tenant's theme to framework defaults. Caller needs UpdateTheme.</summary>
    Task ResetThemeAsync(string tenantId, CancellationToken ct = default);

    /// <summary>
    /// Fetch the CURRENT tenant's theme (no tenant targeting header). Caller needs
    /// MultitenancyPermissions.Tenants.ViewTheme. Used by the tenant-facing dashboard.
    /// </summary>
    Task<TenantThemeDto> GetCurrentThemeAsync(CancellationToken ct = default);

    /// <summary>Save the CURRENT tenant's theme (no tenant targeting header). Caller needs UpdateTheme.</summary>
    Task UpdateCurrentThemeAsync(TenantThemeDto theme, CancellationToken ct = default);

    /// <summary>Reset the CURRENT tenant's theme to framework defaults. Caller needs UpdateTheme.</summary>
    Task ResetCurrentThemeAsync(CancellationToken ct = default);
}
