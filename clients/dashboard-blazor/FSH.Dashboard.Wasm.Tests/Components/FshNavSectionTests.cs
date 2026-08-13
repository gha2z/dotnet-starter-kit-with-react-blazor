using System.Linq;
using Bunit;
using FSH.BlazorShared.Components;
using MudBlazor;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshNavSectionTests : TestSetup
{
    [Fact]
    public void Expanded_State_Renders_Header_With_Caption()
    {
        var cut = Render<FshNavSection>(parameters => parameters
            .Add(x => x.SectionId, "identity")
            .Add(x => x.Caption, "Identity")
            .Add(x => x.Icon, Icons.Material.Filled.People)
            .AddChildContent("<span>child</span>"));

        cut.Find(".fsh-nav-section-header").TextContent.ShouldContain("Identity");
        // bUnit normalizes aria-expanded as a boolean attribute (false ⇒ attribute removed)
        cut.Find(".fsh-nav-section-header").Attributes.Any(a => a.Name == "aria-expanded").ShouldBeFalse();
    }

    [Fact]
    public void Open_Section_Gets_Open_Class_On_Header_And_Body()
    {
        var cut = Render<FshNavSection>(parameters => parameters
            .Add(x => x.SectionId, "identity")
            .Add(x => x.IsOpen, true)
            .AddChildContent("<span>child</span>"));

        cut.Find(".fsh-nav-section").ClassList.ShouldContain("open");
        cut.Find(".fsh-nav-section-body").ClassList.ShouldContain("open");
        // bUnit normalizes aria-expanded as a boolean attribute (true ⇒ bare/empty attribute)
        cut.Find(".fsh-nav-section-header").Attributes.Any(a => a.Name == "aria-expanded").ShouldBeTrue();
    }

    [Fact]
    public void Clicking_Header_Invokes_OnToggle_With_SectionId()
    {
        string? toggled = null;
        var cut = Render<FshNavSection>(parameters => parameters
            .Add(x => x.SectionId, "identity")
            .Add(x => x.OnToggle, id => toggled = id)
            .AddChildContent("<span>child</span>"));

        cut.Find(".fsh-nav-section-header").Click();

        toggled.ShouldBe("identity");
    }

    [Fact]
    public void Collapsed_Mode_Renders_Flat_Item_Stack_Without_Header()
    {
        var cut = Render<FshNavSection>(parameters => parameters
            .Add(x => x.SectionId, "identity")
            .Add(x => x.Caption, "Identity")
            .Add(x => x.Collapsed, true)
            .AddChildContent("<span>item</span>"));

        cut.FindAll(".fsh-nav-section-header").ShouldBeEmpty();
        cut.Find(".fsh-nav-section-stack").ShouldNotBeNull();
        cut.Find(".fsh-nav-section-stack").TextContent.ShouldBe("item");
    }
}
