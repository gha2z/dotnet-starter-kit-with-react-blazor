namespace FSH.BlazorShared.Models.Identity;

public sealed record RoleDto(
    string? Id,
    string? Name,
    string? Description,
    IReadOnlyCollection<string>? Permissions);

/// <summary>Create (Id = "") or update an existing role's name and description.</summary>
public sealed record UpsertRoleRequest(
    string Id,
    string Name,
    string? Description);

/// <summary>Replace the set of permissions assigned to a role.</summary>
public sealed record UpdateRolePermissionsRequest(
    string RoleId,
    List<string> Permissions);

/// <summary>One entry in the host-wide permission catalog. Mirrors the server's
/// <c>PermissionCatalogEntryDto</c> — the API is the authoritative source.</summary>
public sealed record PermissionCatalogEntryDto(
    string Name,
    string Description,
    string Resource,
    string Action,
    bool IsBasic,
    bool IsRoot);
