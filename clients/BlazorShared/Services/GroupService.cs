using System.Net.Http.Json;
using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public sealed class GroupService(HttpClient http) : IGroupService
{
    private const string GroupsBase = "/api/v1/identity/groups";

    public async Task<List<GroupDto>> ListAsync(string? search = null, CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(search)
            ? string.Empty
            : $"?search={Uri.EscapeDataString(search.Trim())}";
        return await http.GetFromJsonAsync<List<GroupDto>>($"{GroupsBase}{query}", ct) ?? [];
    }

    public async Task<GroupDto> GetByIdAsync(Guid groupId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<GroupDto>($"{GroupsBase}/{groupId}", ct)
            ?? throw new InvalidOperationException("Null group response");
    }

    public async Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(GroupsBase, request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GroupDto>(ct)
            ?? throw new InvalidOperationException("Null create group response");
    }

    public async Task<GroupDto> UpdateAsync(UpdateGroupRequest request, CancellationToken ct = default)
    {
        var response = await http.PutAsJsonAsync($"{GroupsBase}/{request.Id}", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GroupDto>(ct)
            ?? throw new InvalidOperationException("Null update group response");
    }

    public async Task DeleteAsync(Guid groupId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{GroupsBase}/{groupId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<GroupMemberDto>> GetMembersAsync(Guid groupId, CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<GroupMemberDto>>($"{GroupsBase}/{groupId}/members", ct) ?? [];
    }

    public async Task<AddUsersToGroupResult> AddUsersAsync(Guid groupId, IReadOnlyList<string> userIds, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{GroupsBase}/{groupId}/members", new { userIds }, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AddUsersToGroupResult>(ct)
            ?? new AddUsersToGroupResult(0, []);
    }

    public async Task RemoveUserAsync(Guid groupId, string userId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{GroupsBase}/{groupId}/members/{userId}", ct);
        response.EnsureSuccessStatusCode();
    }
}
