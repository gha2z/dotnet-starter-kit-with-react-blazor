using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface IRoleService
{
    Task<List<RoleDto>> ListAsync(CancellationToken ct = default);
    Task<RoleDto> GetAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto> GetWithPermissionsAsync(string roleId, CancellationToken ct = default);
    Task<RoleDto> UpsertAsync(UpsertRoleRequest request, CancellationToken ct = default);
    Task DeleteAsync(string roleId, CancellationToken ct = default);
    Task<string> UpdatePermissionsAsync(UpdateRolePermissionsRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PermissionCatalogEntryDto>> GetPermissionCatalogAsync(CancellationToken ct = default);
}
