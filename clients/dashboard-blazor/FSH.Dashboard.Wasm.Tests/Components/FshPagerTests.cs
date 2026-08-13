using Bunit;
using FSH.BlazorShared.Components;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshPagerTests : TestSetup
{
    [Fact]
    public void Shows_Summary_Range_And_Folio()
    {
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 42)
            .Add(x => x.PageNumber, 1)
            .Add(x => x.PageSize, 10));

        cut.Markup.ShouldContain("Showing 1–10 of 42");
        cut.Markup.ShouldContain("folio 1/5");
    }

    [Fact]
    public void Single_Item_Shows_Singular_Summary()
    {
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 1)
            .Add(x => x.PageNumber, 1)
            .Add(x => x.PageSize, 10));

        cut.Markup.ShouldContain("Showing 1 of 1");
        cut.Markup.ShouldContain("folio 1/1");
    }

    [Fact]
    public void First_Page_Disables_Previous_Button()
    {
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 42)
            .Add(x => x.PageNumber, 1)
            .Add(x => x.PageSize, 10));

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons.Count.ShouldBe(2);
        buttons[0].HasAttribute("disabled").ShouldBeTrue();
        buttons[1].HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public void Last_Page_Disables_Next_Button()
    {
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 42)
            .Add(x => x.PageNumber, 5)
            .Add(x => x.PageSize, 10));

        var buttons = cut.FindAll("button.mud-icon-button");
        buttons[0].HasAttribute("disabled").ShouldBeFalse();
        buttons[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void Clicking_Next_Invokes_PageChanged_With_Next_Page()
    {
        int? nextPage = null;
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 42)
            .Add(x => x.PageNumber, 1)
            .Add(x => x.PageSize, 10)
            .Add(x => x.PageChanged, page => nextPage = page));

        cut.FindAll("button.mud-icon-button")[1].Click();

        nextPage.ShouldBe(2);
    }

    [Fact]
    public void Empty_Result_Renders_Nothing()
    {
        var cut = Render<FshPager>(parameters => parameters
            .Add(x => x.TotalCount, 0)
            .Add(x => x.PageNumber, 1)
            .Add(x => x.PageSize, 10));

        cut.Markup.ShouldNotContain("folio");
    }
}
