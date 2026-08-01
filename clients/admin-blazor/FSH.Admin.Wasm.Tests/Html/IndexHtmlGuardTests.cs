using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Html;

/// <summary>
/// Guards the <c>&lt;base href="/" /&gt;</c> ordering in both WASM apps' index.html.
/// If the base tag appears AFTER the &lt;link&gt; tags, a full page load at a sub-route
/// (e.g. /users/... or /roles/...) resolves relative asset URLs against the document URL
/// and 404s (GET /users/_content/MudBlazor/... ERR_ABORTED 404).
/// </summary>
public sealed class IndexHtmlGuardTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "clients")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName
            ?? throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }

    private static string ReadIndexHtml(string relativePath)
    {
        var fullPath = Path.Combine(RepoRoot(), relativePath);
        File.Exists(fullPath).ShouldBeTrue($"index.html not found at {fullPath}");
        return File.ReadAllText(fullPath);
    }

    [Theory]
    [InlineData("clients/admin-blazor/FSH.Admin.Wasm/wwwroot/index.html")]
    [InlineData("clients/dashboard-blazor/FSH.Dashboard.Wasm/wwwroot/index.html")]
    public void Base_href_precedes_all_link_tags(string indexPath)
    {
        var html = ReadIndexHtml(indexPath);

        var baseIndex = html.IndexOf("<base href=\"/\"", StringComparison.Ordinal);
        var firstLinkIndex = html.IndexOf("<link", StringComparison.Ordinal);

        baseIndex.ShouldBeGreaterThanOrEqualTo(0, "missing <base href=\"/\" /> in <head>");
        firstLinkIndex.ShouldBeGreaterThanOrEqualTo(0, "expected at least one <link> in <head>");
        baseIndex.ShouldBeLessThan(firstLinkIndex,
            "base href must precede <link> tags so sub-route full loads resolve assets from the root");
    }
}
