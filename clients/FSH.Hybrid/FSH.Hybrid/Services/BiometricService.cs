using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public interface IBiometricService
{
    Task<bool> IsAvailableAsync(CancellationToken ct = default);
    Task<bool> AuthenticateAsync(string reason, CancellationToken ct = default);
}

public sealed class BiometricService : IBiometricService
{
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
#if WINDOWS
        return IsAvailableWindowsAsync();
#elif ANDROID
        return IsAvailableAndroidAsync();
#elif IOS || MACCATALYST
        return IsAvailableAppleAsync();
#else
        return Task.FromResult(false);
#endif
    }

    public Task<bool> AuthenticateAsync(string reason, CancellationToken ct = default)
    {
#if WINDOWS
        return AuthenticateWindowsAsync(reason);
#elif ANDROID
        return AuthenticateAndroidAsync(reason, ct);
#elif IOS || MACCATALYST
        return AuthenticateAppleAsync(reason);
#else
        return Task.FromResult(false);
#endif
    }

#if WINDOWS
    private static async Task<bool> IsAvailableWindowsAsync()
    {
        var availability = await Windows.Security.Credentials.UI.UserConsentVerifier.CheckAvailabilityAsync();
        return availability == Windows.Security.Credentials.UI.UserConsentVerifierAvailability.Available;
    }

    private static async Task<bool> AuthenticateWindowsAsync(string reason)
    {
        var result = await Windows.Security.Credentials.UI.UserConsentVerifier.RequestVerificationAsync(reason);
        return result == Windows.Security.Credentials.UI.UserConsentVerificationResult.Verified;
    }
#endif

#if ANDROID
    private static Task<bool> IsAvailableAndroidAsync()
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is null)
        {
            return Task.FromResult(false);
        }

        var biometricManager = AndroidX.Biometric.BiometricManager.From(activity);
        var canAuthenticate = biometricManager.CanAuthenticate(AndroidX.Biometric.BiometricManager.Authenticators.BiometricStrong);
        return Task.FromResult(canAuthenticate == AndroidX.Biometric.BiometricManager.BiometricSuccess);
    }

    private static Task<bool> AuthenticateAndroidAsync(string reason, CancellationToken ct)
    {
        var activity = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity;
        if (activity is not AndroidX.Fragment.App.FragmentActivity fragmentActivity)
        {
            return Task.FromResult(false);
        }

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var executor = AndroidX.Core.Content.ContextCompat.GetMainExecutor(activity);
        if (executor is null)
        {
            return Task.FromResult(false);
        }

        var callback = new BiometricAuthCallback(tcs);
        var prompt = new AndroidX.Biometric.BiometricPrompt(fragmentActivity, executor, callback);
        var promptInfo = new AndroidX.Biometric.BiometricPrompt.PromptInfo.Builder()
            .SetTitle("Unlock FSH Hybrid")
            .SetSubtitle(reason)
            .SetNegativeButtonText("Cancel")
            .Build();

        ct.Register(() => tcs.TrySetCanceled(ct));
        prompt.Authenticate(promptInfo);
        return tcs.Task;
    }

    private sealed class BiometricAuthCallback(TaskCompletionSource<bool> tcs) : AndroidX.Biometric.BiometricPrompt.AuthenticationCallback
    {
        public override void OnAuthenticationSucceeded(AndroidX.Biometric.BiometricPrompt.AuthenticationResult result)
            => tcs.TrySetResult(true);

        public override void OnAuthenticationFailed()
        {
            // Failed attempts do not dismiss the prompt; keep waiting for success or cancel.
        }

        public override void OnAuthenticationError(int errorCode, Java.Lang.ICharSequence message)
            => tcs.TrySetResult(false);
    }
#endif

#if IOS || MACCATALYST
    private static async Task<bool> IsAvailableAppleAsync()
    {
        var context = new LocalAuthentication.LAContext();
        return context.CanEvaluatePolicy(
            LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
            out _);
    }

    private static async Task<bool> AuthenticateAppleAsync(string reason)
    {
        var context = new LocalAuthentication.LAContext();
        if (!context.CanEvaluatePolicy(LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics, out _))
        {
            return false;
        }

        try
        {
            var (success, _) = await context.EvaluatePolicyAsync(
                LocalAuthentication.LAPolicy.DeviceOwnerAuthenticationWithBiometrics,
                reason);
            return success;
        }
        catch (Foundation.NSErrorException)
        {
            return false;
        }
    }
#endif
}
