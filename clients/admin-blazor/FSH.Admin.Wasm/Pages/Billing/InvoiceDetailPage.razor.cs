using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Billing;

public sealed partial class InvoiceDetailPage
{
    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private InvoiceDto? _invoice;
    private string? _error;
    private bool _loading = true;

    private DateTime? _dueAt;
    private string _voidReason = string.Empty;
    private bool _isDownloading;
    private bool _isIssuing;
    private bool _isPaying;
    private bool _isVoiding;

    private string Status => _invoice?.Status ?? string.Empty;

    private string HeaderDescription
    {
        get
        {
            if (_invoice is null)
            {
                return "Loading invoice…";
            }

            var parts = new List<string>
            {
                $"tenant {_invoice.TenantId}",
                $"period {FshFormat.Period(_invoice.PeriodYear, _invoice.PeriodMonth)}",
                $"created {FshFormat.DateShort(_invoice.CreatedAtUtc)}",
            };
            if (_invoice.PeriodStartUtc is not null && _invoice.PeriodEndUtc is not null)
            {
                parts.Add($"term {FshFormat.DateShort(_invoice.PeriodStartUtc)} – {FshFormat.DateShort(_invoice.PeriodEndUtc)}");
            }

            if (_invoice.IssuedAtUtc is not null)
            {
                parts.Add($"issued {FshFormat.DateShort(_invoice.IssuedAtUtc)}");
            }

            if (_invoice.DueAtUtc is not null && Status == "Issued")
            {
                parts.Add($"due {FshFormat.DateShort(_invoice.DueAtUtc)}");
            }

            if (_invoice.PaidAtUtc is not null)
            {
                parts.Add($"paid {FshFormat.DateShort(_invoice.PaidAtUtc)}");
            }

            if (_invoice.VoidedAtUtc is not null)
            {
                parts.Add($"voided {FshFormat.DateShort(_invoice.VoidedAtUtc)}");
            }

            return string.Join(" · ", parts);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!Guid.TryParse(Id, out var invoiceId))
        {
            _error = "Invalid invoice identifier.";
            _loading = false;
            return;
        }

        _loading = true;
        _error = null;
        try
        {
            _invoice = await BillingService.GetInvoiceByIdAsync(invoiceId);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load invoice: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DownloadPdfAsync()
    {
        if (_invoice is null)
        {
            return;
        }

        _isDownloading = true;
        try
        {
            var bytes = await BillingService.GetInvoicePdfAsync(_invoice.Id);
            await Js.InvokeVoidAsync("fshDownload.saveFile", $"{_invoice.InvoiceNumber}.pdf", bytes);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDownloading = false;
        }
    }

    private async Task IssueAsync()
    {
        if (_invoice is null)
        {
            return;
        }

        _isIssuing = true;
        try
        {
            DateTime? dueAtUtc = _dueAt is null ? null : DateTime.SpecifyKind(_dueAt.Value, DateTimeKind.Utc);
            await BillingService.IssueInvoiceAsync(_invoice.Id, dueAtUtc);
            Snackbar.Add("Invoice issued — status moved to Issued.", Severity.Success);
            _dueAt = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Issue failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isIssuing = false;
        }
    }

    private async Task MarkPaidAsync()
    {
        if (_invoice is null)
        {
            return;
        }

        _isPaying = true;
        try
        {
            await BillingService.MarkInvoicePaidAsync(_invoice.Id);
            Snackbar.Add("Marked paid.", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Mark-paid failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isPaying = false;
        }
    }

    private async Task VoidAsync()
    {
        if (_invoice is null)
        {
            return;
        }

        _isVoiding = true;
        try
        {
            await BillingService.VoidInvoiceAsync(_invoice.Id, string.IsNullOrWhiteSpace(_voidReason) ? null : _voidReason.Trim());
            Snackbar.Add("Invoice voided.", Severity.Success);
            _voidReason = string.Empty;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Void failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isVoiding = false;
        }
    }

    private void GoBackAsync() => Nav.NavigateTo("/billing/invoices");

    private static Color LineItemTone(string kind) => kind switch
    {
        "BaseFee" => Color.Default,
        "Overage" => Color.Warning,
        "Adjustment" => Color.Default,
        _ => Color.Default,
    };
}
