using Bunit;
using FSH.BlazorShared.Components;
using FSH.BlazorShared.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshOfflineBannerTests : TestSetup
{
    private readonly INetworkStatus _network = Substitute.For<INetworkStatus>();

    private IRenderedComponent<FshOfflineBanner> RenderBanner()
    {
        Services.AddSingleton(_network);
        return Render<FshOfflineBanner>();
    }

    [Fact]
    public void Online_renders_no_banner()
    {
        _network.IsOnline.Returns(true);

        var cut = RenderBanner();

        cut.FindAll(".fsh-offline-banner").ShouldBeEmpty();
    }

    [Fact]
    public void Offline_renders_warning_banner_with_offline_copy()
    {
        _network.IsOnline.Returns(false);

        var cut = RenderBanner();

        var banner = cut.FindAll(".fsh-offline-banner").ShouldHaveSingleItem();
        banner.TextContent.ShouldContain("You are offline");
    }

    [Fact]
    public void Banner_appears_when_status_flips_offline()
    {
        _network.IsOnline.Returns(true);
        var cut = RenderBanner();
        cut.FindAll(".fsh-offline-banner").ShouldBeEmpty();

        _network.IsOnline.Returns(false);
        _network.StatusChanged += Raise.Event<Action>();

        cut.FindAll(".fsh-offline-banner").ShouldHaveSingleItem();
    }

    [Fact]
    public void Banner_auto_dismisses_on_reconnect()
    {
        _network.IsOnline.Returns(false);
        var cut = RenderBanner();
        cut.FindAll(".fsh-offline-banner").ShouldHaveSingleItem();

        _network.IsOnline.Returns(true);
        _network.StatusChanged += Raise.Event<Action>();

        cut.FindAll(".fsh-offline-banner").ShouldBeEmpty();
    }

    [Fact]
    public void Unsubscribes_from_status_changed_on_dispose()
    {
        _network.IsOnline.Returns(false);
        var cut = RenderBanner();

        cut.Dispose();

        // Firing after disposal must not throw (component unsubscribed).
        Should.NotThrow(() => _network.StatusChanged += Raise.Event<Action>());
    }
}
