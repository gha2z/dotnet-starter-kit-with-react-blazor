using FSH.BlazorShared.Formatting;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Billing;

public sealed partial class PlansListPage
{
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private List<BillingPlanDto> _plans = [];
    private string? _error;
    private bool _loading = true;

    private int _activeCount => _plans.Count(p => p.IsActive);
    private int _inactiveCount => _plans.Count - _activeCount;
    private decimal _averageBase => _plans.Count == 0 ? 0 : _plans.Sum(p => p.MonthlyBasePrice) / _plans.Count;
    private string _currency => _plans.Count == 0 ? "USD" : _plans[0].Currency;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _plans = await BillingService.GetPlansAsync(includeInactive: true);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load plans: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }

    private static decimal TermPrice(BillingPlanDto plan) =>
        plan.Interval == "Yearly" ? plan.AnnualPrice ?? plan.MonthlyBasePrice * 12 : plan.MonthlyBasePrice;

    private static string OverageLabel(BillingPlanDto plan)
    {
        var entries = plan.OverageRates?
            .Where(kv => kv.Value > 0)
            .Select(kv => $"{kv.Key} {FshFormat.Money(kv.Value, plan.Currency)}")
            .ToList() ?? [];
        return entries.Count == 0 ? "—" : string.Join(" · ", entries);
    }

    private async Task OpenCreateAsync()
    {
        var dialog = await DialogService.ShowAsync<PlanFormDialog>(
            "New plan",
            options: new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add("Plan created", Severity.Success);
            await ReloadAsync();
        }
    }

    private async Task OpenEditAsync(BillingPlanDto plan)
    {
        var parameters = new DialogParameters { { "Plan", plan } };
        var dialog = await DialogService.ShowAsync<PlanFormDialog>(
            "Edit plan",
            parameters,
            new DialogOptions { MaxWidth = MaxWidth.Medium, FullWidth = true, CloseButton = true });
        var result = await dialog.Result;
        if (result is not null && !result.Canceled)
        {
            Snackbar.Add($"Plan \"{plan.Name}\" updated", Severity.Success);
            await ReloadAsync();
        }
    }

    private async Task ReloadAsync()
    {
        _loading = true;
        _error = null;
        try
        {
            _plans = await BillingService.GetPlansAsync(includeInactive: true);
        }
        catch (Exception ex)
        {
            _error = $"Failed to load plans: {ex.Message}";
        }
        finally
        {
            _loading = false;
        }
    }
}
