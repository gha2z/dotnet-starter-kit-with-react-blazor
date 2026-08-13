using Bunit;
using FSH.Admin.Wasm.Pages.Audits;
using FSH.BlazorShared.Models.Audits;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using System.Text.Json;
using Xunit;

namespace FSH.Admin.Wasm.Tests.Pages.Audits;

public class AuditDetailDialogTests : TestSetup
{
    private readonly IAuditService _auditService = Substitute.For<IAuditService>();
    private static readonly Guid AuditId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public AuditDetailDialogTests()
    {
        Services.AddSingleton(_auditService);
    }

    private static AuditDetailDto SampleDetail() => new()
    {
        Id = AuditId,
        OccurredAtUtc = new DateTime(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc),
        ReceivedAtUtc = new DateTime(2026, 7, 2, 9, 0, 1, DateTimeKind.Utc),
        EventType = AuditEventType.EntityChange,
        Severity = AuditSeverity.Warning,
        TenantId = "acme-corp",
        UserId = "user-1",
        UserName = "admin@root",
        TraceId = "trace-abc",
        SpanId = "span-xyz",
        CorrelationId = "corr-123",
        RequestId = "req-456",
        Source = "Catalog",
        Tags = AuditTag.PiiMasked | AuditTag.Authorization,
        Payload = JsonDocument.Parse("""{"entity":"Product","id":"p-1"}""").RootElement.Clone(),
    };

    [Fact]
    public void Identity_section_renders_type_severity_and_source()
    {
        var detail = SampleDetail();

        var cut = Render<AuditIdentitySection>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("EntityChange");
        cut.Markup.ShouldContain("Warning");
        cut.Markup.ShouldContain("Catalog");
    }

    [Fact]
    public void Correlation_section_renders_ids()
    {
        var detail = SampleDetail();

        var cut = Render<AuditCorrelationSection>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("trace-abc");
        cut.Markup.ShouldContain("span-xyz");
        cut.Markup.ShouldContain("corr-123");
        cut.Markup.ShouldContain("req-456");
    }

    [Fact]
    public void Context_section_renders_who_where_when()
    {
        var detail = SampleDetail();

        var cut = Render<AuditContextSection>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("admin@root (user-1)");
        cut.Markup.ShouldContain("acme-corp");
        cut.Markup.ShouldContain("PiiMasked, Authorization");
    }

    [Fact]
    public void Payload_section_renders_pretty_json()
    {
        var detail = SampleDetail();

        var cut = Render<AuditPayloadSection>(p => p.Add(x => x.Detail, detail));

        cut.Markup.ShouldContain("PAYLOAD");
        cut.Markup.ShouldContain("\"entity\"");
    }
}
