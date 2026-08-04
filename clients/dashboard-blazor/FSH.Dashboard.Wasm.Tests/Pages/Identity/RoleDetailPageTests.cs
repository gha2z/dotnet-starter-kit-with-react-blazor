using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class RoleDetailPageTests : TestSetup
{
    private const string RoleId = "r1";

    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public RoleDetailPageTests()
    {
        Services.AddSingleton(_roleService);
    }

    private static RoleDto SampleRole(string id = RoleId) =>
        new(id, "Support", "Inbound support", ["Permissions.Users.View"]);

    private static IReadOnlyList<PermissionCatalogEntryDto> SampleCatalog() =>
        [
            new("Permissions.Users.View", "View users", "Users", "View", true, false),
            new("Permissions.Users.Delete", "Delete users", "Users", "Delete", false, false),
            new("Permissions.Roles.View", "View roles", "Roles", "View", true, false),
        ];

    private void StubLoad()
    {
        _roleService.GetWithPermissionsAsync(RoleId, Arg.Any<CancellationToken>())
            .Returns(SampleRole());
        _roleService.GetPermissionCatalogAsync(Arg.Any<CancellationToken>())
            .Returns(SampleCatalog());
    }

    private IRenderedComponent<FSH.Dashboard.Wasm.Pages.Identity.RoleDetailPage> RenderPage() =>
        Render<FSH.Dashboard.Wasm.Pages.Identity.RoleDetailPage>(p => p.Add(x => x.Id, RoleId));

    [Fact]
    public void Renders_role_profile_and_permission_groups()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(IdentityPermissions.Roles.Update, IdentityPermissions.Roles.Delete);
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Support");
            cut.Markup.ShouldContain("Permission grants");
            cut.Markup.ShouldContain("View users");
            cut.Markup.ShouldContain("Delete users");
        });
    }

    [Fact]
    public void Checked_permission_is_marked()
    {
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.FindAll(".mud-checkbox-input").Count.ShouldBe(3));
        cut.WaitForAssertion(() => cut.FindAll(".mud-checkbox-input[checked]").Count.ShouldBe(1));
    }

    [Fact]
    public void Toggling_a_permission_enables_save()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(IdentityPermissions.Roles.Update);
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.FindAll(".mud-checkbox-input").Count.ShouldBe(3));
        cut.FindAll(".mud-checkbox-input")[1].Change(true);

        cut.WaitForAssertion(() => cut.FindAll("button").Any(b => b.TextContent.Contains("Save permissions")));
        cut.FindAll("button").First(b => b.TextContent.Contains("Save permissions")).Click();

        _roleService.Received(1).UpdatePermissionsAsync(
            Arg.Is<UpdateRolePermissionsRequest>(r => r.Permissions.Contains("Permissions.Users.Delete")),
            Arg.Any<CancellationToken>());
    }
}
