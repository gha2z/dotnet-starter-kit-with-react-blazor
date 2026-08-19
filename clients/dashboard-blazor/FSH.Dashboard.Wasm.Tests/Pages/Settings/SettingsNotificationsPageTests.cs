using Bunit;
using FSH.Dashboard.Wasm.Pages.Settings;
using Microsoft.AspNetCore.Components;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.Settings;

public sealed class SettingsNotificationsPageTests : TestSetup
{
    [Fact]
    public void Renders_header_and_roadmap_copy()
    {
        var cut = Render<SettingsNotificationsPage>();

        cut.Markup.ShouldContain("Notification preferences");
        cut.Markup.ShouldContain("tunable yet");
    }

    [Fact]
    public void Open_bell_button_triggers_bell_js_interop()
    {
        var cut = Render<SettingsNotificationsPage>();

        cut.FindAll("button").First(b => b.TextContent.Contains("Open notifications bell")).Click();

        JSInterop.VerifyInvoke("fshBell.open");
    }
}