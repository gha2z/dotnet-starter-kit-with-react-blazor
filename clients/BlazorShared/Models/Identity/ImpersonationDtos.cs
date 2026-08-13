using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Identity;

[JsonConverter(typeof(JsonStringEnumConverter<ImpersonationGrantStatus>))]
public enum ImpersonationGrantStatus
{
    Active = 0,
    Ended = 1,
    Revoked = 2,
    Expired = 3
}

/// <summary>Response from POST /identity/impersonation/start — the admin never holds
/// the impersonation session; it hands the access token to the dashboard app via URL hash.</summary>
public sealed record ImpersonationResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string ActorUserId,
    string ActorTenantId,
    string ImpersonatedUserId,
    string ImpersonatedTenantId);

/// <summary>Request for POST /identity/impersonation/start (React parity shape).</summary>
public sealed record StartImpersonationRequest(
    string TargetUserId,
    string TargetTenantId,
    string Reason,
    int? DurationMinutes = null);

/// <summary>
/// Impersonation context derived from JWT actor claims (act_sub / act_tenant / act_name).
/// React parity: ImpersonationInfo in clients/dashboard/src/auth/auth-context.ts.
/// </summary>
public sealed record ImpersonationInfo(
    string ActorUserId,
    string ActorTenantId,
    string ActorName,
    string SubjectUserId,
    string SubjectTenantId,
    string SubjectName);

/// <summary>One audit-visible impersonation grant row.</summary>
public sealed record ImpersonationGrantDto(
    Guid Id,
    string Jti,
    string ActorUserId,
    string? ActorUserName,
    string ActorTenantId,
    string ImpersonatedUserId,
    string? ImpersonatedUserName,
    string ImpersonatedTenantId,
    string Reason,
    DateTime StartedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? EndedAtUtc,
    DateTime? RevokedAtUtc,
    string? RevokedByUserId,
    string? RevokedByUserName,
    string? RevokeReason,
    ImpersonationGrantStatus Status);