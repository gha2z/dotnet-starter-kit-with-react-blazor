using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class UserDetailPageTests : TestSetup
{
    private const string UserId = "u1";

    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IImpersonationService _impersonationService = Substitute.For<IImpersonationService>();

    public UserDetailPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_impersonationService);
    }

    private static UserDto SampleUser(string id = UserId) =>
        new(id, "janedoe", "Jane", "Doe", "jane@example.com", true, true, "+1-555-0100", null, false);

    private static List<UserRoleDto> SampleRoles() =>
        [
            new("r1", "Admin", "Platform administrators", false),
            new("r2", "Support", "Inbound support", true),
        ];

    private static List<UserSessionDto> SampleSessions() =>
        [
            new(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                UserId,
                "janedoe",
                "jane@example.com",
                "10.0.0.5",
                "Browser",
                "Chrome",
                "136",
                "Windows",
                "11",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddMinutes(-5),
                DateTime.UtcNow.AddDays(6),
                true,
                true),
        ];

    private void StubLoad(UserDto? user = null)
    {
        _userService.GetAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(user ?? SampleUser());
        _userService.GetRolesAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(SampleRoles());
        _userService.GetSessionsAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(SampleSessions());
    }

    private IRenderedComponent<FSH.Dashboard.Wasm.Pages.Identity.UserDetailPage> RenderPage() =>
        Render<FSH.Dashboard.Wasm.Pages.Identity.UserDetailPage>(p => p.Add(x => x.Id, UserId));

    [Fact]
    public void Renders_user_identity_and_role_assignment()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(FSH.BlazorShared.Permissions.SessionsPermissions.ViewAll);
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Jane Doe");
            cut.Markup.ShouldContain("janedoe");
            cut.Markup.ShouldContain("Role assignment");
            cut.Markup.ShouldContain("Active sessions");
        });
    }

    [Fact]
    public void Toggle_status_marks_user_inactive()
    {
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Deactivate"));
        cut.FindAll("button").First(b => b.TextContent.Contains("Deactivate")).Click();

        _userService.Received(1).ToggleStatusAsync(UserId, false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Shows_pending_badge_when_role_switch_toggled()
    {
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Role assignment"));
        cut.FindAll(".mud-switch input")[0].Change(true);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("pending"));
    }

    [Fact]
    public void Saving_roles_calls_assign_roles()
    {
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Save roles"));
        cut.FindAll(".mud-switch input")[0].Change(true);

        cut.FindAll("button").First(b => b.TextContent.Contains("Save roles")).Click();

        _userService.Received(1).AssignRolesAsync(UserId, Arg.Any<List<UserRoleDto>>(), Arg.Any<CancellationToken>());
    }
}
