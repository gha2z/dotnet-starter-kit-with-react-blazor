using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using NSubstitute;
using Shouldly;
using Xunit;

namespace FSH.Dashboard.Wasm.Tests.Pages.System;

public sealed class AuditsPageTests : TestSetup
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();

    public AuditsPageTests()
    {
        Services.AddSingleton(_auditService);
    }

    [Fact]
    public void Renders_header_and_filter_controls()
    {
        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render<AuditsPage>();

        cut.Markup.ShouldContain("Audit trail");
        cut.Markup.ShouldContain("24h");
        cut.Markup.ShouldContain("7d");
    }

    [Fact]
    public void Shows_empty_state_when_no_audits()
    {
        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render<AuditsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("No audit events"));
    }

    [Fact]
    public void Renders_audit_rows_when_data_present()
    {
        var audits = new List<AuditSummaryDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = DateTime.UtcNow.AddMinutes(-5),
                EventType = AuditEventType.Activity,
                Severity = AuditSeverity.Information,
                Source = "CatalogService",
                UserName = "admin@acme.com",
            },
        };

        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>(audits, 1, 25, 1, 1, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto
            {
                EventsByType = new Dictionary<string, long> { ["Activity"] = 1 },
            });

        var cut = Render<AuditsPage>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("CatalogService"));
        cut.Markup.ShouldContain("admin@acme.com");
    }

    [Fact]
    public void Default_request_excludes_system_activity()
    {
        ListAuditsRequest? captured = null;
        _auditService.ListAsync(
                Arg.Do<ListAuditsRequest>(r => captured = r),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render<AuditsPage>();

        cut.WaitForAssertion(() => captured.ShouldNotBeNull());
        captured!.ExcludeEventType.ShouldBe(AuditEventType.Activity);
    }

    [Fact]
    public void Toggling_hide_activity_removes_exclusion()
    {
        var captured = new List<ListAuditsRequest>();
        _auditService.ListAsync(
                Arg.Do<ListAuditsRequest>(r => captured.Add(r)),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render<AuditsPage>();
        cut.WaitForAssertion(() => captured.ShouldHaveSingleItem());

        cut.Find("input.mud-switch-input").Change(false);

        cut.WaitForAssertion(() => captured.Count.ShouldBe(2));
        captured[1].ExcludeEventType.ShouldBeNull();
    }

    [Fact]
    public void Advanced_filters_render_after_toggle()
    {
        _auditService.ListAsync(Arg.Any<ListAuditsRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render<AuditsPage>();
        cut.WaitForAssertion(() => cut.FindAll("input").Count.ShouldBeGreaterThan(0));

        cut.FindAll("button").First(b => b.TextContent.Contains("Advanced filters")).Click();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Correlation id");
            cut.Markup.ShouldContain("Trace id");
            cut.Markup.ShouldContain("Tags");
            cut.Markup.ShouldContain("Hide advanced filters");
        });
    }

    [Fact]
    public void Tags_filter_is_sent_as_bitmask()
    {
        var captured = new List<ListAuditsRequest>();
        _auditService.ListAsync(
                Arg.Do<ListAuditsRequest>(r => captured.Add(r)),
                Arg.Any<CancellationToken>())
            .Returns(new PagedResult<AuditSummaryDto>([], 1, 25, 0, 0, false, false));
        _auditService.GetSummaryAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(new AuditSummaryAggregateDto());

        var cut = Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<AuditsPage>(1);
            builder.CloseComponent();
        });
        cut.WaitForAssertion(() => captured.ShouldHaveSingleItem());

        captured[0].Tags.ShouldBeNull();

        cut.FindAll("button").First(b => b.TextContent.Contains("Advanced filters")).Click();
        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Tags"));

        var tagSelect = cut.FindAll("div.mud-input-control").First(d => d.TextContent.Contains("Tags"));
        tagSelect.MouseDown(new MouseEventArgs { Detail = 1, Button = 0 });
        cut.WaitForAssertion(() => cut.FindAll("div.mud-list-item").Count.ShouldBeGreaterThan(0));
        cut.FindAll("div.mud-list-item").First(i => i.TextContent.Trim() == "Auth").Click();

        cut.WaitForAssertion(() => captured.Count.ShouldBe(2));
        captured[1].Tags.ShouldNotBeNull();
        (captured[1].Tags!.Value & AuditTag.Authentication).ShouldNotBe(AuditTag.None);
    }
}
