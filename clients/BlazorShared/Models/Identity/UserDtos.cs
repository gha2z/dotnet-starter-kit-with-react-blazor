namespace FSH.BlazorShared.Models.Identity;

public sealed record UserDto(
    string? Id,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    string? ImageUrl,
    bool TwoFactorEnabled);

public sealed record UserRoleDto(
    string? RoleId,
    string? RoleName,
    string? Description,
    bool Enabled);

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string UserName,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber);

public sealed record RegisterUserResponse(string? UserId, string? Message);

public sealed record AssignUserRolesRequest(string UserId, List<UserRoleDto> UserRoles);

public sealed record UserSessionDto(
    Guid Id,
    string? UserId,
    string? UserName,
    string? UserEmail,
    string? IpAddress,
    string? DeviceType,
    string? Browser,
    string? BrowserVersion,
    string? OperatingSystem,
    string? OsVersion,
    DateTime CreatedAt,
    DateTime LastActivityAt,
    DateTime ExpiresAt,
    bool IsActive,
    bool IsCurrentSession);
