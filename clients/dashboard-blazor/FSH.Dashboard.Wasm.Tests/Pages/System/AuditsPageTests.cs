using Bunit;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Services;
using FSH.Dashboard.Wasm.Pages.SystemPages;
using Microsoft.Extensions.DependencyInjection;
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
}
