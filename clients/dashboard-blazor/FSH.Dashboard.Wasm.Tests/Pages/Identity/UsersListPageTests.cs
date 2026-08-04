using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class UsersListPageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public UsersListPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_roleService);
    }

    private static PagedResult<UserDto> Page(params UserDto[] users) =>
        new([.. users], 1, 25, users.Length, users.Length == 0 ? 0 : 1, false, false);

    private static UserDto SampleUser(
        string id = "u1",
        string userName = "janedoe",
        string? firstName = "Jane",
        string? lastName = "Doe",
        string email = "jane@example.com",
        bool isActive = true,
        bool emailConfirmed = true) =>
        new(id, userName, firstName, lastName, email, isActive, emailConfirmed, "+1-555-0100", null, false);

    private void StubSearch(params UserDto[] users)
    {
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page(users));
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([]);
    }

    [Fact]
    public void Renders_users_with_name_username_and_status()
    {
        StubSearch(
            SampleUser("u1", "janedoe", "Jane", "Doe", "jane@example.com"),
            SampleUser("u2", "bobsmith", "Bob", "Smith", "bob@example.com", isActive: false));

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.UsersListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Users");
            cut.Markup.ShouldContain("Jane Doe");
            cut.Markup.ShouldContain("Bob Smith");
            cut.Markup.ShouldContain("janedoe");
        });
    }

    [Fact]
    public void Shows_empty_state_when_no_users()
    {
        StubSearch();

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.UsersListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No users yet"));
    }
}
