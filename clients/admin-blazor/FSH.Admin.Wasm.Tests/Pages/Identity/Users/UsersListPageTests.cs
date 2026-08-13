using Bunit;
using FSH.Admin.Wasm.Pages.Identity.Users;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Identity.Users;

public class UsersListPageTests : TestSetup
{
    private readonly IUserService _userService = Substitute.For<IUserService>();
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public UsersListPageTests()
    {
        Services.AddSingleton(_userService);
        Services.AddSingleton(_roleService);
    }

    private static UserDto SampleUser(string id, string username, string firstName, string lastName, string email, bool isActive = true, bool emailConfirmed = true) =>
        new(id, username, firstName, lastName, email, isActive, emailConfirmed, null, null, false);

    private static RoleDto SampleRole(string id, string name) =>
        new(id, name, null, []);

    [Fact]
    public void Renders_users_and_roles_loaded_from_services()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns([SampleRole("r1", "Admin"), SampleRole("r2", "User")]);
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserDto>(
                [SampleUser("u1", "jane", "Jane", "Doe", "jane@example.com"), SampleUser("u2", "john", "John", "Smith", "john@example.com")],
                1, 12, 2, 1, false, false));

        var cut = Render<UsersListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jane Doe"));
        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("@john");
            cut.Markup.ShouldContain("jane@example.com");
            cut.Markup.ShouldContain("2 accounts on this tenant.");
        });
    }

    [Fact]
    public void Row_click_navigates_to_user_detail()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserDto>(
                [SampleUser("11111111-1111-1111-1111-111111111111", "jane", "Jane", "Doe", "jane@example.com")],
                1, 12, 1, 1, false, false));
        var cut = Render<UsersListPage>();
        cut.WaitForAssertion(() => cut.FindAll("tbody tr.mud-table-row").Count.ShouldBe(1));
        cut.Find("tbody tr.mud-table-row").Click();

        var nav = Services.GetRequiredService<Bunit.TestDoubles.BunitNavigationManager>();
        nav.History.Last().Uri.ShouldEndWith("/users/11111111-1111-1111-1111-111111111111");
    }

    [Fact]
    public void Search_input_reloads_table_with_debounce()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserDto>([], 1, 12, 0, 0, false, false));

        var cut = Render<UsersListPage>();
        cut.WaitForAssertion(() => _userService.Received(1).SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>()));

        cut.Find("input").Change("jane");

        cut.WaitForAssertion(
            () => _userService.Received().SearchAsync(
                Arg.Is<SearchRequest>(r => r.Search == "jane"),
                Arg.Any<CancellationToken>()),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Filter_changes_reload_with_filters()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>()).Returns([SampleRole("r1", "Admin")]);
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserDto>([], 1, 12, 0, 0, false, false));

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<UsersListPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => _userService.Received(1).SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>()));

        cut.FindAll(".fsh-segmented-btn").First(b => b.TextContent.Trim() == "Active").Click();

        cut.WaitForAssertion(
            () => _userService.Received().SearchAsync(
                Arg.Is<SearchRequest>(r =>
                    r.Filters != null
                    && r.Filters["IsActive"] == "true"
                    && r.Filters["EmailConfirmed"] == null
                    && r.Filters["RoleId"] == null),
                Arg.Any<CancellationToken>()),
            TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Role_load_failure_shows_alert()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromException<List<RoleDto>>(new InvalidOperationException("boom")));
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<UserDto>([], 1, 12, 0, 0, false, false));

        var cut = Render<UsersListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load roles: boom"));
    }

    [Fact]
    public void User_load_failure_shows_alert()
    {
        _roleService.ListAsync(Arg.Any<CancellationToken>()).Returns([]);
        _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<UserDto>>(new InvalidOperationException("nope")));

        var cut = Render<UsersListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Failed to load users: nope"));
    }
}
