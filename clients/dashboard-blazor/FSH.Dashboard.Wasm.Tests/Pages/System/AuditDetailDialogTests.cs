using Bunit;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.System;

public sealed class AuditDetailDialogTests : TestSetup
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public AuditDetailDialogTests()
    {
        Services.AddSingleton(_auditService);
    }

    private static readonly Guid CurrentId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly Guid OtherId = Guid.Parse("55555555-5555-5555-5555-555555555555");

    private static AuditDetailDto SampleDetail() => new()
    {
        Id = CurrentId,
        OccurredAtUtc = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
        ReceivedAtUtc = new DateTime(2026, 7, 2, 9, 0, 1, DateTimeKind.Utc),
        EventType = AuditEventType.Security,
        Severity = AuditSeverity.Error,
        TenantId = "acme-corp",
        UserId = "user-1",
        UserName = "admin@acme.com",
        TraceId = "trace-abc",
        CorrelationId = "corr-123",
        Source = "Identity",
        Tags = AuditTag.Authentication,
        Payload = JsonDocument.Parse("""{"event":"login","result":"fail"}""").RootElement.Clone(),
    };

    private async Task<IRenderedComponent<MudDialogProvider>> ShowDialogAsync(AuditDetailDto detail)
    {
        var provider = Render<MudDialogProvider>();
        var dialogService = Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { { "AuditDetail", detail } };
        _ = await dialogService.ShowAsync<AuditDetailDialog>("Audit detail", parameters);
        return provider;
    }

    [Fact]
    public async Task Renders_metadata_and_payload_copy()
    {
        _auditService.GetByCorrelationAsync(Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AuditSummaryDto>());

        var provider = await ShowDialogAsync(SampleDetail());

        provider.WaitForAssertion(() =>
        {
            provider.Markup.ShouldContain("Security");
            provider.Markup.ShouldContain("Identity");
            provider.Markup.ShouldContain("PAYLOAD");
            provider.Markup.ShouldContain("\"login\"");
        });
        provider.Markup.ShouldContain("Copy");
    }

    [Fact]
    public async Task Renders_related_events_timeline()
    {
        var current = SampleDetail();
        _auditService.GetByCorrelationAsync("corr-123", Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AuditSummaryDto>
            {
                new()
                {
                    Id = OtherId,
                    OccurredAtUtc = new DateTime(2026, 7, 2, 9, 0, 2, DateTimeKind.Utc),
                    EventType = AuditEventType.Security,
                    Severity = AuditSeverity.Information,
                    Source = "Identity",
                    UserName = "admin@acme.com",
                },
                new()
                {
                    Id = CurrentId,
                    OccurredAtUtc = current.OccurredAtUtc,
                    EventType = current.EventType,
                    Severity = current.Severity,
                    Source = "Identity",
                },
            });

        var provider = await ShowDialogAsync(current);

        provider.WaitForAssertion(() => provider.Markup.ShouldContain("RELATED EVENTS"));
        provider.Markup.ShouldContain("2 on this correlation");
        provider.Markup.ShouldContain("this event");
        provider.Markup.ShouldContain("+2s");
    }

    [Fact]
    public async Task Shows_message_when_no_other_events_share_correlation()
    {
        _auditService.GetByCorrelationAsync("corr-123", Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AuditSummaryDto>());

        var provider = await ShowDialogAsync(SampleDetail());

        provider.WaitForAssertion(() =>
            provider.Markup.ShouldContain("No other events share this correlation."));
    }

    [Fact]
    public async Task Copy_writes_payload_to_clipboard()
    {
        _auditService.GetByCorrelationAsync(Arg.Any<string>(), Arg.Any<DateTime?>(), Arg.Any<DateTime?>(), Arg.Any<CancellationToken>())
            .Returns(new List<AuditSummaryDto>());

        var provider = await ShowDialogAsync(SampleDetail());
        provider.WaitForAssertion(() => provider.Markup.ShouldContain("Copy"));

        provider.FindAll("button").First(b => b.TextContent.Trim() == "Copy").Click();

        provider.WaitForAssertion(() => JSInterop.VerifyInvoke("fshClipboard.copy"));
    }
}
