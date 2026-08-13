using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public sealed class RoleService(HttpClient http) : IRoleService
{
    private const string IdentityBase = "/api/v1/identity";

    public async Task<List<RoleDto>> ListAsync(CancellationToken ct = default)
    {
        var result = await http.GetFromJsonAsync<PagedResult<RoleDto>>(
            $"{IdentityBase}/roles?PageNumber=1&PageSize=100", ct);
        return result?.Items ?? [];
    }

    public async Task<RoleDto> GetAsync(string roleId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<RoleDto>($"{IdentityBase}/roles/{roleId}", ct)
            ?? throw new InvalidOperationException("Null role response");
    }

    public async Task<RoleDto> GetWithPermissionsAsync(string roleId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<RoleDto>($"{IdentityBase}/{roleId}/permissions", ct)
            ?? throw new InvalidOperationException("Null role response");
    }

    public async Task<RoleDto> UpsertAsync(UpsertRoleRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/roles", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RoleDto>(ct)
            ?? throw new InvalidOperationException("Null upsert response");
    }

    public async Task DeleteAsync(string roleId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{IdentityBase}/roles/{roleId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> UpdatePermissionsAsync(UpdateRolePermissionsRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{IdentityBase}/{request.RoleId}/permissions", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<IReadOnlyList<PermissionCatalogEntryDto>> GetPermissionCatalogAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<IReadOnlyList<PermissionCatalogEntryDto>>(
            $"{IdentityBase}/permissions/catalog", ct) ?? [];
    }
}
