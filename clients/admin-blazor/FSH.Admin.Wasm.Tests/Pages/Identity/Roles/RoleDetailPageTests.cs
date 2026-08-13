using Bunit;
using FSH.Admin.Wasm.Pages.Identity.Roles;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Roles;

public class RoleDetailPageTests : TestSetup
{
    private const string RoleId = "11111111-1111-1111-1111-111111111111";

    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public RoleDetailPageTests()
    {
        Services.AddSingleton(_roleService);
    }

    private static RoleDto SampleRole(string name, IReadOnlyCollection<string>? permissions, string? description = null) =>
        new(RoleId, name, description, permissions);

    private static IReadOnlyList<PermissionCatalogEntryDto> SampleCatalog() =>
    [
        new("Permissions.Users.View", "View users", "Users", "View", true, false),
        new("Permissions.Users.Create", "Create users", "Users", "Create", false, false),
        new("Permissions.Roles.View", "View roles", "Roles", "View", true, false),
        new("Permissions.Roles.Delete", "Delete roles", "Roles", "Delete", false, false),
    ];

    private void StubRole(RoleDto role) =>
        _roleService.GetWithPermissionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(role);

    private void StubCatalog() =>
        _roleService.GetPermissionCatalogAsync(Arg.Any<CancellationToken>()).Returns(SampleCatalog());

    private IRenderedComponent<RoleDetailPage> RenderPage() =>
        Render<RoleDetailPage>(p => p.Add(p => p.Id, RoleId));

    [Fact]
    public void Renders_profile_and_grouped_permission_catalog()
    {
        StubRole(SampleRole("Support", ["Permissions.Users.View", "Permissions.Users.Create"]));
        StubCatalog();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("View users"));
        cut.Markup.ShouldContain("Support");
        cut.Markup.ShouldContain("Delete roles");
        cut.Markup.ShouldContain("Permissions.Users.View");
        cut.Markup.ShouldContain("All changes saved");
        cut.Markup.ShouldContain("02 of 04 granted");
        cut.Markup.ShouldContain("Clear all");
        cut.Markup.ShouldContain("Select all");
        cut.FindAll("input[type=checkbox]").Count.ShouldBe(4);
    }

    [Fact]
    public void Toggle_permission_marks_dirty_and_saves()
    {
        StubRole(SampleRole("Support", ["Permissions.Users.View", "Permissions.Users.Create"]));
        StubCatalog();

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("All changes saved"));

        cut.FindAll("input[type=checkbox]")[0].Change(false);

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Unsaved changes"));
        cut.Markup.ShouldContain("01 of 04 granted");

        cut.FindAll("button").First(b => b.TextContent.Contains("Save permissions")).Click();

        var call = _roleService.ReceivedCalls()
            .Single(c => c.GetMethodInfo().Name == nameof(IRoleService.UpdatePermissionsAsync));
        var request = (UpdateRolePermissionsRequest)call.GetArguments()[0]!;
        request.RoleId.ShouldBe(RoleId);
        request.Permissions.ShouldBe(["Permissions.Users.Create"]);
    }

    [Fact]
    public void Select_all_toggles_whole_group()
    {
        StubRole(SampleRole("Support", []));
        StubCatalog();

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Select all"));

        while (cut.FindAll("button").Any(b => b.TextContent.Contains("Select all")))
        {
            cut.FindAll("button").First(b => b.TextContent.Contains("Select all")).Click();
        }

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Unsaved changes"));
        cut.Markup.ShouldContain("04 of 04 granted");

        while (cut.FindAll("button").Any(b => b.TextContent.Contains("Clear all")))
        {
            cut.FindAll("button").First(b => b.TextContent.Contains("Clear all")).Click();
        }

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("00 of 04 granted"));
    }

    [Fact]
    public void Profile_save_calls_upsert()
    {
        StubRole(SampleRole("Support", null, "Inbound support"));
        StubCatalog();
        _roleService.UpsertAsync(Arg.Any<UpsertRoleRequest>(), Arg.Any<CancellationToken>())
            .Returns(new RoleDto(RoleId, "Support", "Inbound support", null));

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Save profile"));

        cut.FindAll("input[type=text]")[0].Change("Support team");
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Support team"));

        cut.FindAll("button").First(b => b.TextContent.Contains("Save profile")).Click();

        _roleService.Received(1).UpsertAsync(
            Arg.Is<UpsertRoleRequest>(r =>
                r.Id == RoleId && r.Name == "Support team" && r.Description == "Inbound support"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void System_role_is_read_only()
    {
        StubRole(SampleRole("Admin", ["Permissions.Roles.View"]));
        StubCatalog();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Built-in role — read only"));
        cut.Markup.ShouldNotContain("Danger zone");
        cut.Markup.ShouldContain("System");
        cut.FindAll("input[type=text]").Count.ShouldBe(2);
        cut.FindAll("input[type=text]")[0].HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("button").First(b => b.TextContent.Contains("Save profile")).HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Delete_requires_typed_name()
    {
        StubRole(SampleRole("Support", null, "Inbound support"));
        StubCatalog();

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Danger zone"));

        var deleteButton = () => cut.FindAll("button").First(b => b.TextContent.Contains("Delete role"));
        deleteButton().HasAttribute("disabled").ShouldBeTrue();

        cut.FindAll("input[type=text]")[2].Change("Support");

        cut.WaitForAssertion(() => deleteButton().HasAttribute("disabled").ShouldBeFalse());
        deleteButton().Click();

        await _roleService.Received(1).DeleteAsync(RoleId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_with_wrong_confirmation_is_blocked()
    {
        StubRole(SampleRole("Support", null));
        StubCatalog();

        var cut = RenderPage();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Danger zone"));

        cut.FindAll("input[type=text]")[2].Change("Supportt");

        var deleteButton = cut.FindAll("button").First(b => b.TextContent.Contains("Delete role"));
        deleteButton.HasAttribute("disabled").ShouldBeTrue();
        deleteButton.Click();

        await _roleService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Load_failure_shows_alert()
    {
        _roleService.GetWithPermissionsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<RoleDto>(new InvalidOperationException("nope")));
        _roleService.GetPermissionCatalogAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<IReadOnlyList<PermissionCatalogEntryDto>>(new InvalidOperationException("nope")));

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load role: nope"));
    }
}
