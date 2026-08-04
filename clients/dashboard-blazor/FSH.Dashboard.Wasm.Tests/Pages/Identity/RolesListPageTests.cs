using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class RolesListPageTests : TestSetup
{
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public RolesListPageTests()
    {
        Services.AddSingleton(_roleService);
    }

    private static RoleDto SampleRole(string id, string name, string? description = null, IReadOnlyCollection<string>? permissions = null) =>
        new(id, name, description, permissions);

    [Fact]
    public void Renders_roles_with_names_and_permission_counts()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([
                SampleRole("r1", "Support", "Inbound support", ["Permissions.Users.View", "Permissions.Roles.View"]),
                SampleRole("r2", "Viewer", null, []),
            ]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.RolesListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Roles");
            cut.Markup.ShouldContain("Support");
            cut.Markup.ShouldContain("Viewer");
            cut.Markup.ShouldContain("2");
        });
    }

    [Fact]
    public void Shows_empty_state_when_no_roles()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.RolesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No roles yet"));
    }

    [Fact]
    public void Marks_system_roles_with_badge()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([SampleRole("r1", "Admin", "Built-in"), SampleRole("r2", "Support", null, [])]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.RolesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("system"));
    }
}
