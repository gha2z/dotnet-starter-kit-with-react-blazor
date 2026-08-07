using FSH.BlazorShared.Models;
using FSH.BlazorShared.Models.Webhooks;
using FSH.BlazorShared.Services;
using FSH.BlazorShared.Components;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Webhooks;

public sealed partial class WebhooksListPage
{
    private const int PageSize = 25;

    [Inject] private IWebhookService WebhookService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private List<WebhookSubscriptionDto> _items = [];
    private PagedResult<WebhookSubscriptionDto>? _data;
    private string? _error;
    private bool _loading = true;
    private int _pageNumber = 1;
    private Guid? _busyId;

    protected override async Task OnInitializedAsync()
    {
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _data = await WebhookService.GetSubscriptionsAsync(_pageNumber, PageSize);
            _items = _data.Items;
        }
        catch (Exception ex)
        {
            _error = $"Failed to load subscriptions: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ReloadAsync()
    {
        await LoadAsync();
    }

    private async Task OnPageChangedAsync(int page)
    {
        _pageNumber = Math.Max(1, page);
        await LoadAsync();
    }

    private async Task OpenCreateAsync()
    {
        var options = new DialogOptions { MaxWidth = MaxWidth.Large, FullWidth = true, CloseButton = true };
        var dialog = await DialogService.ShowAsync<WebhookCreateDialog>("New webhook subscription", options);
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Subscription created", Severity.Success);
            await LoadAsync();
        }
    }

    private async Task TestAsync(WebhookSubscriptionDto sub)
    {
        _busyId = sub.Id;
        try
        {
            var success = await WebhookService.TestSubscriptionAsync(sub.Id);
            if (success)
            {
                Snackbar.Add("Test event delivered", Severity.Success);
            }
            else
            {
                Snackbar.Add("Endpoint rejected the test event. See Deliveries for the response code.", Severity.Warning);
            }
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Test failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _busyId = null;
        }
    }

    private async Task DeleteAsync(WebhookSubscriptionDto sub)
    {
        var parameters = new DialogParameters
        {
            { "Message", $"Delete subscription to {sub.Url}? This cannot be undone." },
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

        _busyId = sub.Id;
        try
        {
            await WebhookService.DeleteSubscriptionAsync(sub.Id);
            Snackbar.Add("Subscription deleted", Severity.Success);
            await LoadAsync();
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Delete failed: {ex.Message}", Severity.Error);
        }
        finally
        {
            _busyId = null;
        }
    }
}
