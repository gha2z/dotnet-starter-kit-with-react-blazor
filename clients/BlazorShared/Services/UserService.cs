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

    public async Task<UserDto> GetMyProfileAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<UserDto>($"{IdentityBase}/profile", ct)
            ?? throw new InvalidOperationException("Null profile response");
    }

    public async Task<UserDto> UpdateMyProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{IdentityBase}/profile", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserDto>(ct)
            ?? throw new InvalidOperationException("Null profile response");
    }

    public async Task SetProfileImageAsync(string? imageUrl, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{IdentityBase}/profile/image", new SetProfileImageRequest(imageUrl), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{IdentityBase}/change-password", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{UsersBase}/{userId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ConfirmEmailAsync(string userId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{UsersBase}/{userId}/confirm-email", new { }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task ResendConfirmationEmailAsync(string userId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{UsersBase}/{userId}/resend-confirmation-email", new { }, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RevokeSessionAsync(string userId, Guid sessionId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{UsersBase}/{userId}/sessions/{sessionId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<int> RevokeAllSessionsAsync(string userId, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{UsersBase}/{userId}/sessions/revoke-all", new { }, ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RevokeAllSessionsResponse>(cancellationToken: ct);
        return result?.RevokedCount ?? 0;
    }

    private sealed record RevokeAllSessionsResponse(int RevokedCount);

    public async Task<PagedResult<UserDto>> SearchInTenantAsync(string tenantId, string? search, CancellationToken ct = default)
    {
        var query = $"PageNumber=1&PageSize=25";
        if (!string.IsNullOrWhiteSpace(search))
            query += $"&Search={Uri.EscapeDataString(search.Trim())}";

        var request = new HttpRequestMessage(HttpMethod.Get, $"{UsersBase}/search?{query}");
        request.Headers.TryAddWithoutValidation("tenant", tenantId);

        var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(ct)
            ?? new PagedResult<UserDto>([], 1, 25, 0, 0, false, false);
    }
}
