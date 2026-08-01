using System.Net.Http.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public sealed class UserService(HttpClient http) : IUserService
{
    private const string UsersBase = "/api/v1/identity/users";
    private const string IdentityBase = "/api/v1/identity";

    public async Task<PagedResult<UserDto>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var query = $"PageNumber={request.PageNumber}&PageSize={request.PageSize}";
        if (!string.IsNullOrWhiteSpace(request.Search))
            query += $"&Search={Uri.EscapeDataString(request.Search.Trim())}";
        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            var sort = request.SortDirection == "desc" ? $"-{request.SortBy}" : request.SortBy;
            query += $"&Sort={Uri.EscapeDataString(sort)}";
        }
        if (request.Filters is not null)
        {
            foreach (var (key, value) in request.Filters)
            {
                if (!string.IsNullOrWhiteSpace(value))
                    query += $"&{key}={Uri.EscapeDataString(value)}";
            }
        }

        return await http.GetFromJsonAsync<PagedResult<UserDto>>($"{UsersBase}/search?{query}", ct)
            ?? new PagedResult<UserDto>([], request.PageNumber, request.PageSize, 0, 0, false, false);
    }

    public async Task<UserDto> GetAsync(string userId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<UserDto>($"{UsersBase}/{userId}", ct)
            ?? throw new InvalidOperationException("Null user response");
    }

    public async Task<RegisterUserResponse> CreateAsync(RegisterUserRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/register", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegisterUserResponse>(ct)
            ?? throw new InvalidOperationException("Null register response");
    }

    public async Task ToggleStatusAsync(string userId, bool activate, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"{UsersBase}/{userId}", new { userId, activateUser = activate }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<UserRoleDto>>($"{UsersBase}/{userId}/roles", ct) ?? [];
    }

    public async Task AssignRolesAsync(string userId, List<UserRoleDto> roles, CancellationToken ct = default)
    {
        var request = new AssignUserRolesRequest(userId, roles);
        var response = await http.PostAsJsonAsync($"{UsersBase}/{userId}/roles", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserSessionDto>> GetSessionsAsync(string userId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<UserSessionDto>>($"{UsersBase}/{userId}/sessions", ct) ?? [];
    }
}
