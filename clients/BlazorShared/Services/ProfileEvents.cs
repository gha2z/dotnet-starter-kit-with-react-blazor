namespace FSH.BlazorShared.Services;

/// <summary>
/// Lightweight cross-component signal for "the signed-in user's profile changed"
/// (avatar updated, name/email edited). Raised by the profile settings pages so
/// the topbar user tile can refresh the photo without a full reload.
/// </summary>
public static class ProfileEvents
{
    public static event Action? ProfileUpdated;

    public static void NotifyProfileUpdated() => ProfileUpdated?.Invoke();
}