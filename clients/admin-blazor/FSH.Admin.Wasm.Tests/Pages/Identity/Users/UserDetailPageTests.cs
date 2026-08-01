using Bunit;
using FSH.Admin.Wasm.Pages.Identity.Users;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Users;

public class UserDetailPageTests : TestSetup
{
    private const string UserId = "11111111-1111-1111-1111-111111111111";

    private readonly IUserService _userService = Substitute.For<IUserService>();

    public UserDetailPageTests()
    {
        Services.AddSingleton(_userService);
    }

    private static UserDto SampleUser() =>
        new(UserId, "janedoe", "Jane", "Doe", "jane@example.com", true, true, "+1-555-0100", null, false);

    private static UserSessionDto SampleSession() =>
        new(
            Guid.NewGuid(),
            UserId,
            "janedoe",
            "jane@example.com",
            "203.0.113.7",
            "Desktop",
            "Chrome",
            "131.0",
            "Windows 11",
            "10.0",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow.AddDays(1),
            true,
            false);

    private static void SetupLoad(IRenderedComponent<UserDetailPage> cut) =>
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("jane@example.com"));

    [Fact]
    public void Renders_identity_roles_and_sessions()
    {
        _userService.GetAsync(UserId, Arg.Any<CancellationToken>()).Returns(SampleUser());
        _userService.GetRolesAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([
                new UserRoleDto("r1", "Admin", "Tenant administrators", true),
                new UserRoleDto("r2", "User", null, false),
            ]);
        _userService.GetSessionsAsync(UserId, Arg.Any<CancellationToken>()).Returns([SampleSession()]);

        var cut = Render<UserDetailPage>(p => p.Add(p => p.Id, UserId));
        SetupLoad(cut);

        cut.Markup.ShouldContain("Jane Doe");
        cut.Markup.ShouldContain("Tenant administrators");
        cut.Markup.ShouldContain("Chrome");
        cut.Markup.ShouldContain("203.0.113.7");
        cut.Markup.ShouldContain("Deactivate account");
    }

    [Fact]
    public void Role_toggle_enables_save_and_assigns_roles()
    {
        _userService.GetAsync(UserId, Arg.Any<CancellationToken>()).Returns(SampleUser());
        _userService.GetRolesAsync(UserId, Arg.Any<CancellationToken>())
            .Returns([new UserRoleDto("r1", "Admin", null, false)]);
        _userService.GetSessionsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<UserDetailPage>(p => p.Add(p => p.Id, UserId));
        SetupLoad(cut);

        cut.FindAll("input[type=checkbox]")[0].Change(true);
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("1 pending change"));

        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Save changes"));
        saveButton.Click();

        _userService.Received(1).AssignRolesAsync(
            UserId,
            Arg.Is<List<UserRoleDto>>(roles =>
                roles.Count == 1 && roles[0].RoleId == "r1" && roles[0].Enabled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Status_toggle_deactivates_user()
    {
        _userService.GetAsync(UserId, Arg.Any<CancellationToken>()).Returns(SampleUser());
        _userService.GetRolesAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);
        _userService.GetSessionsAsync(UserId, Arg.Any<CancellationToken>()).Returns([]);

        var cut = Render<UserDetailPage>(p => p.Add(p => p.Id, UserId));
        SetupLoad(cut);

        var toggleButton = cut.FindAll("button").First(b => b.TextContent.Contains("Deactivate account"));
        toggleButton.Click();

        _userService.Received(1).ToggleStatusAsync(UserId, false, Arg.Any<CancellationToken>());
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Activate account"));
        cut.Markup.ShouldContain("Disabled");
    }

    [Fact]
    public void Load_failure_shows_alert()
    {
        _userService.GetAsync(UserId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<UserDto>(new InvalidOperationException("nope")));

        var cut = Render<UserDetailPage>(p => p.Add(p => p.Id, UserId));

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load user: nope"));
    }
}
