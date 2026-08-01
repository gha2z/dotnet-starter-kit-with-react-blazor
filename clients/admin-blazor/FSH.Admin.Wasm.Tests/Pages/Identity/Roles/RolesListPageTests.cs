using Bunit;
using FSH.Admin.Wasm.Pages.Identity.Roles;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Roles;

public class RolesListPageTests : TestSetup
{
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public RolesListPageTests()
    {
        Services.AddSingleton(_roleService);
    }

    private static RoleDto SampleRole(string id, string name, string? description = null, IReadOnlyCollection<string>? permissions = null) =>
        new(id, name, description, permissions);

    [Fact]
    public void Renders_roles_system_first_with_permission_counts()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([
                SampleRole("r1", "User", "Regular accounts"),
                SampleRole("r2", "Admin", "Platform administrators"),
                SampleRole("r3", "Basic", "Minimal access"),
                SampleRole("r4", "Support", "Inbound support", ["Permissions.Users.View", "Permissions.Roles.View"]),
            ]);

        var cut = Render<RolesListPage>();

        cut.WaitForAssertion(() => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(4));
        var rows = cut.FindAll("tbody tr.mud-table-row");
        rows[0].TextContent.ShouldContain("Admin");
        rows[1].TextContent.ShouldContain("Basic");
        rows[2].TextContent.ShouldContain("Support");
        rows[3].TextContent.ShouldContain("User");
        rows[0].TextContent.ShouldContain("System");
        rows[3].TextContent.ShouldNotContain("System");
        rows[2].TextContent.ShouldContain("2 permissions");
        rows[3].TextContent.ShouldContain("—");
        cut.Markup.ShouldContain("4 roles on this tenant.");
    }

    [Fact]
    public void Search_filters_roles_by_name_or_description()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([
                SampleRole("r1", "Admin", "Platform administrators"),
                SampleRole("r2", "Support", "Inbound support"),
                SampleRole("r3", "Auditor", "Read-only access"),
            ]);

        var cut = Render<RolesListPage>();
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(3));

        cut.Find("input").Change("support");

        cut.WaitForAssertion(
            () => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(1),
            TimeSpan.FromSeconds(5));
        cut.Find("tbody tr.mud-table-row").TextContent.ShouldContain("Support");
    }

    [Fact]
    public void Row_click_navigates_to_role_detail()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([SampleRole("11111111-1111-1111-1111-111111111111", "Support")]);

        var cut = Render<RolesListPage>();
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(1));
        cut.Find("tbody tr.mud-table-row").Click();

        var nav = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        nav.History.Last().Uri.ShouldEndWith("/roles/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void New_role_button_opens_create_dialog()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(IdentityPermissions.Roles.Create);
        _roleService.ListAsync(Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudDialogProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<RolesListPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No roles defined yet"));

        cut.FindAll("button").First(b => b.TextContent.Contains("New role")).Click();

        cut.WaitForAssertion(
            () => cut.FindAll("button").Any(b => b.TextContent.Contains("Create role")));
        cut.Markup.ShouldContain("Description (optional)");
    }

    [Fact]
    public void Load_failure_shows_alert()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<RoleDto>>(new InvalidOperationException("boom")));

        var cut = Render<RolesListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load roles: boom"));
    }
}
