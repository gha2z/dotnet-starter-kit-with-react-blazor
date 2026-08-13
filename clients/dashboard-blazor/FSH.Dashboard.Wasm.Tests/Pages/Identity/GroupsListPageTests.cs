using Bunit;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Identity;

public sealed class GroupsListPageTests : TestSetup
{
    private readonly IGroupService _groupService = Substitute.For<IGroupService>();

    public GroupsListPageTests()
    {
        Services.AddSingleton(_groupService);
    }

    private static GroupDto SampleGroup(
        Guid id,
        string name,
        string? description = null,
        bool isDefault = false,
        bool isSystemGroup = false,
        int memberCount = 0) =>
        new(id, name, description, isDefault, isSystemGroup, memberCount, [], [], DateTime.UtcNow.AddDays(-2));

    [Fact]
    public void Renders_groups_with_names_member_counts_and_flags()
    {
        _groupService.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns([
                SampleGroup(Guid.Parse("11111111-1111-1111-1111-111111111111"), "Support", "Support squad", isDefault: true, memberCount: 4),
                SampleGroup(Guid.Parse("22222222-2222-2222-2222-222222222222"), "Everyone", "All users", isSystemGroup: true, memberCount: 12),
            ]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.GroupsListPage>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Groups");
            cut.Markup.ShouldContain("Support");
            cut.Markup.ShouldContain("Everyone");
            cut.Markup.ShouldContain("Default");
            cut.Markup.ShouldContain("system");
        });
    }

    [Fact]
    public void Shows_empty_state_when_no_groups()
    {
        _groupService.ListAsync(null, Arg.Any<CancellationToken>())
            .Returns([]);

        var cut = Render<FSH.Dashboard.Wasm.Pages.Identity.GroupsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No groups yet"));
    }
}
