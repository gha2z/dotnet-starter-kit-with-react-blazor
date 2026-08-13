using Microsoft.AspNetCore.Components;

namespace FSH.BlazorShared.Infrastructure;

public static class NavigationExtensions
{
    public static bool TryGetQueryString<T>(this NavigationManager nav, string key, out T? value)
    {
        var uri = nav.ToAbsoluteUri(nav.Uri);
        var query = uri.Query.TrimStart('?');
        if (string.IsNullOrEmpty(query))
        {
            value = default;
            return false;
        }

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2 && Uri.UnescapeDataString(parts[0]).Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                var raw = Uri.UnescapeDataString(parts[1]);
                try
                {
                    value = (T)Convert.ChangeType(raw, typeof(T));
                    return true;
                }
                catch
                {
                    value = default;
                    return false;
                }
            }
        }

        value = default;
        return false;
    }
}
