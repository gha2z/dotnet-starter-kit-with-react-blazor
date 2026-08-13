using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Invoices;

public sealed partial class InvoiceDetailPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public string Id { get; set; } = string.Empty;

    private InvoiceDto? _invoice;
    private bool _loading = true;
    private string? _error;
    private bool _downloading;

    protected override async Task OnInitializedAsync() => await LoadAsync();

    private async Task LoadAsync()
    {
        _loading = true;
        try
        {
            _invoice = await BillingService.GetInvoiceByIdAsync(Guid.Parse(Id));
        }
        catch (Exception ex)
        {
            _error = ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task DownloadPdfAsync()
    {
        if (_downloading || _invoice is null) return;
        _downloading = true;
        try
        {
            var bytes = await BillingService.GetInvoicePdfAsync(_invoice.Id);
            await JS.InvokeVoidAsync("fshDownload.saveFile", $"{_invoice.InvoiceNumber}.pdf", bytes);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Download failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _downloading = false;
        }
    }

    private void GoBack() => Navigation.NavigateTo("/invoices");

    private static string FormatPeriod(InvoiceDto invoice) => $"{invoice.PeriodYear}-{invoice.PeriodMonth:00}";

    private static Color StatusTone(string status) => status switch
    {
        "Paid" => Color.Success,
        "Issued" => Color.Info,
        "Void" => Color.Error,
        _ => Color.Default,
    };

    private static Color RowTone(string label) => label switch
    {
        "Due" => Color.Warning,
        "Paid" => Color.Success,
        "Voided" => Color.Error,
        _ => Color.Default,
    };

    [Inject] private IJSRuntime JS { get; set; } = default!;
}
