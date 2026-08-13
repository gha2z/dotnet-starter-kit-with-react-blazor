using Bunit;
using FSH.BlazorShared.Components;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Components;

public sealed class FshErrorBandTests : TestSetup
{
    [Fact]
    public void Renders_message_in_alert()
    {
        var cut = Render<FshErrorBand>(parameters => parameters
            .Add(x => x.Message, "Failed to load data."));

        var band = cut.Find("div[role=alert]");
        band.TextContent.ShouldContain("FAILURE");
        band.TextContent.ShouldContain("Failed to load data.");
    }

    [Fact]
    public void Correlation_id_is_rendered_when_provided()
    {
        var cut = Render<FshErrorBand>(parameters => parameters
            .Add(x => x.Message, "Internal server error.")
            .Add(x => x.CorrelationId, "req-abc-123"));

        cut.Markup.ShouldContain("Request req-abc-123");
    }

    [Fact]
    public void Correlation_id_is_omitted_when_null()
    {
        var cut = Render<FshErrorBand>(parameters => parameters
            .Add(x => x.Message, "Internal server error."));

        cut.Markup.ShouldNotContain("Request ");
    }
}
