using FSH.BlazorShared.Auth;

namespace FSH.Dashboard.Wasm.Auth;

/// <summary>
/// Cross-app impersonation handoff (React parity: impersonation-handoff.ts).
/// The admin app issues an impersonation access token server-side, then opens
/// the dashboard with the token in the URL hash:
///
///   #impersonate?token=&lt;jwt&gt;&amp;tenant=&lt;id&gt;&amp;expiresAt=&lt;iso&gt;
///
/// The hash (not query) is used so the token never leaks via referrer headers
/// or server access logs. Called during App bootstrap — before the Router's
/// first render — so the impersonation session exists before any protected
/// route asks for auth state.
/// </summary>
public sealed class ImpersonationHandoff(ITokenStore tokenStore, AuthStateProvider authState)
{
    /// <summary>
    /// Parses the fragment and installs the impersonation session. Returns true
    /// when a token was installed, false when the hash was absent/malformed
    /// (the caller still strips the fragment in both cases).
    /// </summary>
    public async Task<bool> InstallFromHashAsync(string? hash)
    {
        if (hash is null || !hash.StartsWith("#impersonate?", StringComparison.Ordinal))
        {
            return false;
        }

        var token = GetParam(hash, "token");
        var tenant = GetParam(hash, "tenant");
        if (string.IsNullOrEmpty(token))
        {
            // Malformed handoff - let the normal sign-in flow take over.
            return false;
        }

        // beginImpersonation stashes the currently-installed actor tokens (if any) before
        // swapping. In the typical cross-app handoff there are none - this is a fresh tab -
        // so the stash is a no-op. When the user later clicks End impersonation, the server
        // mints a real actor token+refresh for the operator's account.
        if (await tokenStore.GetAccessTokenAsync() is not null)
        {
            await tokenStore.StashTokensAsync();
        }

        // Impersonation sessions carry no refresh token (React parity). NotifyLoginAsync
        // installs the token + tenant, resets the permission cache and re-raises auth state
        // so the banner/shell render for the impersonated identity.
        await authState.NotifyLoginAsync(token, refreshToken: null, tenant ?? string.Empty);
        return true;
    }

    private static string? GetParam(string hash, string name)
    {
        var queryStart = hash.IndexOf('?');
        if (queryStart < 0)
        {
            return null;
        }

        foreach (var part in hash[(queryStart + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            if (string.Equals(part[..eq], name, StringComparison.Ordinal))
            {
                return Uri.UnescapeDataString(part[(eq + 1)..]);
            }
        }

        return null;
    }
}
