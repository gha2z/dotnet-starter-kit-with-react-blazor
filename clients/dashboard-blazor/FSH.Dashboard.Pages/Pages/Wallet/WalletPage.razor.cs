using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Dashboard.Wasm.Pages.Wallet;

public sealed partial class WalletPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private NavigationManager Navigation { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private WalletDto? _wallet;
    private List<TopupRequestDto> _topupRequests = new();
    private int _pageNumber = 1;
    private int _totalPages = 1;
    private const int PageSize = 20;

    private bool _loadingWallet = true;
    private bool _loadingRequests = true;
    private string? _walletError;
    private string? _requestsError;

    // Top-up form
    private string _amount = string.Empty;
    private string _note = string.Empty;
    private bool _submitting;
    private bool _formSuccess;

    private string Currency => _wallet?.Currency ?? "USD";

    private const decimal LowBalanceThreshold = 10m;

    protected override async Task OnInitializedAsync()
    {
        var walletTask = LoadWalletAsync();
        var requestsTask = LoadRequestsAsync();
        await Task.WhenAll(walletTask, requestsTask);
    }

    private async Task LoadWalletAsync()
    {
        try
        {
            _wallet = await BillingService.GetMyWalletAsync();
        }
        catch (Exception ex)
        {
            _walletError = ex.Message;
        }
        finally
        {
            _loadingWallet = false;
        }
    }

    private async Task LoadRequestsAsync()
    {
        try
        {
            var page = await BillingService.GetTopupRequestsAsync(pageNumber: _pageNumber, pageSize: PageSize);
            _topupRequests = page.Items.ToList();
            _totalPages = page.TotalPages;
        }
        catch (Exception ex)
        {
            _requestsError = ex.Message;
        }
        finally
        {
            _loadingRequests = false;
        }
    }

    private bool IsValidForm => decimal.TryParse(_amount, out var val) && val > 0 && !_submitting;

    private async Task SubmitTopupAsync()
    {
        if (!IsValidForm) return;
        _submitting = true;
        _formSuccess = false;
        try
        {
            var amount = decimal.Parse(_amount);
            await BillingService.CreateTopupRequestAsync(new CreateTopupRequestRequest(amount, string.IsNullOrWhiteSpace(_note) ? null : _note));
            Snackbar.Add("Top-up requested. We'll raise an invoice to credit your wallet shortly.", Severity.Success);
            _amount = string.Empty;
            _note = string.Empty;
            _formSuccess = true;
            await LoadWalletAsync();
            await LoadRequestsAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Top-up request failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _submitting = false;
        }
    }

    private async Task GoToPage(int page)
    {
        _pageNumber = Math.Clamp(page, 1, _totalPages);
        await LoadRequestsAsync();
    }

    private static string StatusTone(string status) => status switch
    {
        "Pending" => "warning",
        "Invoiced" => "info",
        "Completed" => "success",
        "Rejected" => "danger",
        _ => "default",
    };

    private static Color StatusColor(string status) => status switch
    {
        "Pending" => Color.Warning,
        "Invoiced" => Color.Info,
        "Completed" => Color.Success,
        "Rejected" => Color.Error,
        _ => Color.Default,
    };

    private void OpenInvoice(Guid id) => Navigation.NavigateTo($"/invoices/{id}");
}
