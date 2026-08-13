using System.Net.Http.Json;
using System.Text.Json;
using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Audits;

namespace FSH.BlazorShared.Services;

public sealed class AuditService(HttpClient http) : IAuditService
{
    private const string AuditsBase = "/api/v1/audits";

    public async Task<PagedResult<AuditSummaryDto>> ListAsync(ListAuditsRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(request);
        var page = await http.GetFromJsonAsync<PagedResult<AuditSummaryDto>>(
            $"{AuditsBase}/?{query}", ct) ?? EmptyPage<AuditSummaryDto>(request.PageSize ?? 25);
        foreach (var item in page.Items)
        {
            // Server serializes audit enums as integers (no string converter registered).
            // System.Text.Json maps numeric values to enum members, so the only thing to
            // fix here is any values that came through as raw numbers in the JSON.
            Normalize(item);
        }

        return page;
    }

    public async Task<AuditDetailDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var dto = await http.GetFromJsonAsync<AuditDetailDto>($"{AuditsBase}/{id}", ct)
            ?? throw new InvalidOperationException("Audit event not found.");
        Normalize(dto);
        return dto;
    }

    public async Task<AuditSummaryAggregateDto> GetSummaryAsync(string? tenantId = null, CancellationToken ct = default)
    {
        var query = string.IsNullOrWhiteSpace(tenantId) ? string.Empty : $"?TenantId={Uri.EscapeDataString(tenantId)}";
        var summary = await http.GetFromJsonAsync<AuditSummaryAggregateDto>($"{AuditsBase}/summary{query}", ct)
            ?? new AuditSummaryAggregateDto();
        summary.EventsByType = NormalizeDictionary(summary.EventsByType, k => CoerceEventType(k).ToString());
        summary.EventsBySeverity = NormalizeDictionary(summary.EventsBySeverity, k => CoerceSeverity(k).ToString());
        return summary;
    }

    public async Task<IReadOnlyList<AuditSummaryDto>> GetByCorrelationAsync(
        string correlationId,
        DateTime? fromUtc = null,
        DateTime? toUtc = null,
        CancellationToken ct = default)
    {
        var parts = new List<string>();
        if (fromUtc is not null)
        {
            parts.Add($"FromUtc={Uri.EscapeDataString(fromUtc.Value.ToString("O"))}");
        }

        if (toUtc is not null)
        {
            parts.Add($"ToUtc={Uri.EscapeDataString(toUtc.Value.ToString("O"))}");
        }

        var query = parts.Count == 0 ? string.Empty : $"?{string.Join("&", parts)}";
        var items = await http.GetFromJsonAsync<List<AuditSummaryDto>>(
                $"{AuditsBase}/by-correlation/{Uri.EscapeDataString(correlationId)}{query}", ct)
            ?? [];
        foreach (var item in items)
        {
            Normalize(item);
        }

        return items;
    }

    private static void Normalize(AuditSummaryDto dto)
    {
        // Defensive: if the JSON used integer-backed string keys (e.g. tags), ensure the
        // enum props hold valid members. No-op for the normal numeric-enum path.
        dto.EventType = CoerceEventType((int)dto.EventType);
        dto.Severity = CoerceSeverity((int)dto.Severity);
    }

    private static AuditEventType CoerceEventType(int raw) =>
        Enum.IsDefined(typeof(AuditEventType), raw) ? (AuditEventType)raw : AuditEventType.None;

    private static AuditSeverity CoerceSeverity(int raw) =>
        Enum.IsDefined(typeof(AuditSeverity), raw) ? (AuditSeverity)raw : AuditSeverity.None;

    private static AuditEventType CoerceEventType(string raw) =>
        int.TryParse(raw, out var n) ? CoerceEventType(n) : Enum.TryParse<AuditEventType>(raw, true, out var e) ? e : AuditEventType.None;

    private static AuditSeverity CoerceSeverity(string raw) =>
        int.TryParse(raw, out var n) ? CoerceSeverity(n) : Enum.TryParse<AuditSeverity>(raw, true, out var s) ? s : AuditSeverity.None;

    private static Dictionary<string, long> NormalizeDictionary(
        Dictionary<string, long> source,
        Func<string, string> coerce)
    {
        // The server keys the summary histograms by the same integer enum form. Translate
        // them to the string union so the UI can index by name (React parity).
        var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in source)
        {
            var normalized = coerce(key) ?? key;
            result[normalized] = result.GetValueOrDefault(normalized) + value;
        }

        return result;
    }

    private static string BuildQuery(ListAuditsRequest request)
    {
        var parts = new List<string>
        {
            $"PageNumber={request.PageNumber ?? 1}",
            $"PageSize={request.PageSize ?? 25}",
        };

        if (!string.IsNullOrWhiteSpace(request.Sort))
        {
            parts.Add($"Sort={Uri.EscapeDataString(request.Sort)}");
        }

        if (request.FromUtc is not null)
        {
            parts.Add($"FromUtc={Uri.EscapeDataString(request.FromUtc.Value.ToString("O"))}");
        }

        if (request.ToUtc is not null)
        {
            parts.Add($"ToUtc={Uri.EscapeDataString(request.ToUtc.Value.ToString("O"))}");
        }

        AddIfNotEmpty(parts, "TenantId", request.TenantId);
        AddIfNotEmpty(parts, "UserId", request.UserId);
        AddIfNotEmpty(parts, "Source", request.Source);
        AddIfNotEmpty(parts, "CorrelationId", request.CorrelationId);
        AddIfNotEmpty(parts, "TraceId", request.TraceId);
        AddIfNotEmpty(parts, "Search", request.Search);

        if (request.EventType is not null)
        {
            parts.Add($"EventType={request.EventType}");
        }

        if (request.ExcludeEventType is not null)
        {
            parts.Add($"ExcludeEventType={request.ExcludeEventType}");
        }

        if (request.Severity is not null)
        {
            parts.Add($"Severity={request.Severity}");
        }

        if (request.Tags is not null && request.Tags.Value != AuditTag.None)
        {
            // The backend binds the flags enum as a numeric bitmask (React parity).
            parts.Add($"Tags={(int)request.Tags.Value}");
        }

        return string.Join("&", parts);
    }

    private static void AddIfNotEmpty(List<string> parts, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parts.Add($"{key}={Uri.EscapeDataString(value)}");
        }
    }

    private static PagedResult<T> EmptyPage<T>(int pageSize) =>
        new([], 1, pageSize, 0, 0, false, false);
}
