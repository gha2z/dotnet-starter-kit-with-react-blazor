using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface IUserService
{
    Task<PagedResult<UserDto>> SearchAsync(SearchRequest request, CancellationToken ct = default);
    Task<UserDto> GetAsync(string userId, CancellationToken ct = default);
    Task<RegisterUserResponse> CreateAsync(RegisterUserRequest request, CancellationToken ct = default);
    Task ToggleStatusAsync(string userId, bool activate, CancellationToken ct = default);
    Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken ct = default);
    Task AssignRolesAsync(string userId, List<UserRoleDto> roles, CancellationToken ct = default);
    Task<List<UserSessionDto>> GetSessionsAsync(string userId, CancellationToken ct = default);
    Task<UserDto> GetMyProfileAsync(CancellationToken ct = default);
    Task SetProfileImageAsync(string? imageUrl, CancellationToken ct = default);
    Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
    Task<PagedResult<UserDto>> SearchInTenantAsync(string tenantId, string? search, CancellationToken ct = default);
}
