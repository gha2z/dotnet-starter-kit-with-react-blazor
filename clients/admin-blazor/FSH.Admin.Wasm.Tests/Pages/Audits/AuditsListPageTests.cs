using Bunit;
using FSH.Admin.Wasm.Pages.Audits;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Permissions;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Audits;

public class AuditsListPageTests : TestSetup
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public AuditsListPageTests()
    {
        Services.AddSingleton(_auditService);
    }

    private static AuditSummaryDto SampleEvent(
        string source = "Identity",
        AuditEventType type = AuditEventType.Security,
        AuditSeverity severity = AuditSeverity.Information) =>
        new()
        {
            Id = Guid.NewGuid(),
            OccurredAtUtc = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
            EventType = type,
            Severity = severity,
            TenantId = "tenant-1",
            UserName = "admin@root",
            CorrelationId = "corr-123",
            Source = source,
        };

    private static PagedResult<AuditSummaryDto> Page(params AuditSummaryDto[] events) =>
        new([.. events], 1, 25, events.Length, events.Length == 0 ? 0 : 1, false, false);

    private static void StubSummary(IAuditService auditService, AuditSummaryAggregateDto summary) =>
        auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(summary);

    private void StubList(params AuditSummaryDto[] events)
    {
        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Page(events));
    }

    [Fact]
    public void Renders_events_with_source_type_and_timestamp()
    {
        StubList(SampleEvent());
        StubSummary(_auditService, new AuditSummaryAggregateDto());

        var cut = Render<AuditsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Identity"));
        cut.Markup.ShouldContain("Security");
        cut.Markup.ShouldContain("corr-123");
    }

    [Fact]
    public void Shows_empty_state_when_no_events()
    {
        StubList();
        StubSummary(_auditService, new AuditSummaryAggregateDto());

        var cut = Render<AuditsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No audit events match your filters."));
    }

    [Fact]
    public void Shows_error_band_when_load_fails()
    {
        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<AuditSummaryDto>>(new Exception("boom")));
        StubSummary(_auditService, new AuditSummaryAggregateDto());

        var cut = Render<AuditsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("boom"));
    }

    [Fact]
    public void Shows_stat_strip_from_summary()
    {
        StubList(SampleEvent());
        StubSummary(_auditService, new AuditSummaryAggregateDto
        {
            EventsByType = new Dictionary<string, long>
            {
                [nameof(AuditEventType.Security)] = 42,
            },
            EventsBySeverity = new Dictionary<string, long>
            {
                [nameof(AuditSeverity.Error)] = 7,
            },
        });

        var cut = Render<AuditsListPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("42"));
        cut.Markup.ShouldContain("7");
        cut.Markup.ShouldContain("Total events");
        cut.Markup.ShouldContain("Errors + critical");
    }

    [Fact]
    public void Open_detail_opens_dialog()
    {
        StubList(SampleEvent());
        StubSummary(_auditService, new AuditSummaryAggregateDto());

        var cut = Render<AuditsListPage>();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Security"));

        cut.Find("div[role='button']").Click();

        cut.WaitForAssertion(() => cut.FindAll("div").Any(d => d.ClassList.Contains("mud-dialog")));
    }
}
