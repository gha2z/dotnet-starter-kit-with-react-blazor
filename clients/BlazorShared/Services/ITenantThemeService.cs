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
}
