using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class GroupDetailPageTests : TestSetup
{
    private static readonly Guid GroupId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly IGroupService _groupService = Substitute.For<IGroupService>();
    private readonly IRoleService _roleService = Substitute.For<IRoleService>();

    public GroupDetailPageTests()
    {
        Services.AddSingleton(_groupService);
        Services.AddSingleton(_roleService);
    }

    private static GroupDto SampleGroup(string name = "Support", bool isDefault = false, bool isSystemGroup = false) =>
        new(GroupId, name, "Support squad", isDefault, isSystemGroup, 2, ["r1"], ["Support"], DateTime.UtcNow.AddDays(-2));

    private static List<GroupMemberDto> SampleMembers() =>
        [
            new("u1", "janedoe", "jane@example.com", "Jane", "Doe", DateTime.UtcNow.AddDays(-3), "admin@root"),
            new("u2", "bobsmith", "bob@example.com", "Bob", "Smith", DateTime.UtcNow.AddDays(-1), "admin@root"),
        ];

    private static List<RoleDto> SampleRoles() =>
        [new("r1", "Support", "Inbound support", null), new("r2", "Viewer", null, null)];

    private void StubLoad(GroupDto? group = null)
    {
        _groupService.GetByIdAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(group ?? SampleGroup());
        _groupService.GetMembersAsync(GroupId, Arg.Any<CancellationToken>())
            .Returns(SampleMembers());
        _roleService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(SampleRoles());
    }

    private IRenderedComponent<FSH.Dashboard.Wasm.Pages.Identity.GroupDetailPage> RenderPage() =>
        Render<FSH.Dashboard.Wasm.Pages.Identity.GroupDetailPage>(p => p.Add(x => x.Id, GroupId.ToString()));

    [Fact]
    public void Renders_group_hero_roles_and_members()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(GroupsPermissions.Update);
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Support");
            cut.Markup.ShouldContain("Group details");
            cut.Markup.ShouldContain("Jane Doe");
            cut.Markup.ShouldContain("Bob Smith");
            cut.Markup.ShouldContain("members");
        });
    }

    [Fact]
    public void Removing_a_member_calls_remove_user()
    {
        Authorization.SetAuthorized("admin");
        Authorization.SetPolicies(GroupsPermissions.Update);
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.FindAll("[aria-label='Remove member']").Count.ShouldBe(2));
        cut.FindAll("[aria-label='Remove member']").First().Click();

        _groupService.Received(1).RemoveUserAsync(GroupId, "u1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Editing_name_enables_save()
    {
        StubLoad();

        var cut = RenderPage();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Save group"));
        cut.FindAll("input").First().Change("Support v2");

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("1 pending"));
        cut.FindAll("button").First(b => b.TextContent.Contains("Save group")).Click();

        _groupService.Received(1).UpdateAsync(
            Arg.Is<UpdateGroupRequest>(r => r.Name == "Support v2"),
            Arg.Any<CancellationToken>());
    }
}
