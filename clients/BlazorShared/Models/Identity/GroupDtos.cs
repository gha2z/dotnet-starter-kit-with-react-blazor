namespace FSH.BlazorShared.Models.Identity;

public sealed record GroupDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    bool IsSystemGroup,
    int MemberCount,
    IReadOnlyCollection<string>? RoleIds,
    IReadOnlyCollection<string>? RoleNames,
    DateTimeOffset CreatedAt);

public sealed record GroupMemberDto(
    string UserId,
    string? UserName,
    string? Email,
    string? FirstName,
    string? LastName,
    DateTime AddedAt,
    string? AddedBy);

/// <summary>Create (Id = Guid.Empty) or update an existing group.</summary>
public sealed record CreateGroupRequest(
    string Name,
    string? Description,
    bool IsDefault,
    List<string>? RoleIds);

public sealed record UpdateGroupRequest(
    Guid Id,
    string Name,
    string? Description,
    bool IsDefault,
    List<string>? RoleIds);

public sealed record AddUsersToGroupRequest(Guid GroupId, IReadOnlyList<string> UserIds);

public sealed record AddUsersToGroupResult(int AddedCount, List<string> AlreadyMemberUserIds);
