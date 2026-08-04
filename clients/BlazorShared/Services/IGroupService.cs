using FSH.BlazorShared.Models.Identity;

namespace FSH.BlazorShared.Services;

public interface IGroupService
{
    Task<List<GroupDto>> ListAsync(string? search = null, CancellationToken ct = default);
    Task<GroupDto> GetByIdAsync(Guid groupId, CancellationToken ct = default);
    Task<GroupDto> CreateAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<GroupDto> UpdateAsync(UpdateGroupRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid groupId, CancellationToken ct = default);
    Task<List<GroupMemberDto>> GetMembersAsync(Guid groupId, CancellationToken ct = default);
    Task<AddUsersToGroupResult> AddUsersAsync(Guid groupId, IReadOnlyList<string> userIds, CancellationToken ct = default);
    Task RemoveUserAsync(Guid groupId, string userId, CancellationToken ct = default);
}
