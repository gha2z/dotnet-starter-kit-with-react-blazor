namespace FSH.BlazorShared.Models.Identity;

/// <summary>Change-password request (React parity: current + new + confirm).</summary>
public sealed record ChangePasswordRequest(
    string Password,
    string NewPassword,
    string ConfirmNewPassword);

/// <summary>Set the current user's avatar URL (null clears).</summary>
public sealed record SetProfileImageRequest(string? ImageUrl);

/// <summary>Response from the 2FA enroll endpoint.</summary>
public sealed record TwoFactorEnrollmentResponse(string SharedKey, string AuthenticatorUri);
