using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Webhooks;

public sealed partial class WebhookDetailPage
{
    private const int PageSize = 25;

    [Parameter] public string Id { get; set; } = string.Empty;

    [Inject] private IWebhookService WebhookService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private WebhookSubscriptionDto? _subscription;
    private PagedResult<WebhookDeliveryDto>? _deliveries;
    private string? _error;
    private string? _deliveryError;
    private bool _loading = true;
    private bool _loadingDeliveries = true;
    private bool _isTesting;
    private bool _isDeleting;
    private int _deliveryPage = 1;

    private WebhookSubscriptionDto? Subscription => _subscription;

    protected override async Task OnInitializedAsync()
    {
        if (!Guid.TryParse(Id, out var subscriptionId))
        {
            _error = "Invalid subscription identifier.";
            _loading = false;
            return;
        }

        try
        {
            // No GET /subscriptions/{id} on the server, so list with a big enough page
            // and find by id. Subscription counts are typically small.
            var subscriptions = await WebhookService.GetSubscriptionsAsync(1, 200);
            _subscription = subscriptions.Items.FirstOrDefault(s => s.Id == subscriptionId);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load subscription: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }

        if (_subscription is not null)
        {
            await ReloadDeliveriesAsync();
        }
    }

    private async Task ReloadDeliveriesAsync()
    {
        if (!Guid.TryParse(Id, out var subscriptionId))
        {
            return;
        }

        _loadingDeliveries = true;
        _deliveryError = null;
        try
        {
            _deliveries = await WebhookService.GetDeliveriesAsync(subscriptionId, _deliveryPage, PageSize);
        }
        catch (Exception ex)
        {
            _deliveryError = ex.Message;
        }
        finally
        {
            _loadingDeliveries = false;
        }
    }

    private async Task OnDeliveryPageChangedAsync(int page)
    {
        _deliveryPage = Math.Max(1, page);
        await ReloadDeliveriesAsync();
    }

    private async Task TestAsync()
    {
        _isTesting = true;
        try
        {
            var success = await WebhookService.TestSubscriptionAsync(Guid.Parse(Id));
            Snackbar.Add(success ? "Test event delivered" : "Endpoint rejected the test event", success ? Severity.Success : Severity.Warning);
            await ReloadDeliveriesAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Test failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isTesting = false;
        }
    }

    private async Task DeleteAsync()
    {
        var parameters = new DialogParameters
        {
            { "Message", $"Delete subscription to {_subscription?.Url}? This cannot be undone." },
            { "ConfirmText", "Delete" },
            { "CancelText", "Cancel" },
        };
        var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.ExtraSmall };
        var dialog = await DialogService.ShowAsync<FshConfirmDialogContent>("Delete subscription", parameters, options);
        var result = await dialog.Result;
        if (result is null || result.Canceled)
        {
            return;
        }

        _isDeleting = true;
        try
        {
            await WebhookService.DeleteSubscriptionAsync(Guid.Parse(Id));
            Snackbar.Add("Subscription deleted", Severity.Success);
            Nav.NavigateTo("/webhooks");
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private void GoBack() => Nav.NavigateTo("/webhooks");
}
