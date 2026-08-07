using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Billing;

public sealed partial class PlanFormDialog
{
    private static readonly Regex KeyPattern = new(
        "^[a-z0-9][a-z0-9-]{0,62}[a-z0-9]$",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    public sealed class PlanForm
    {
        [Required(ErrorMessage = "Key is required.")]
        public string Key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(64, ErrorMessage = "Keep under 64 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Currency is required.")]
        public string Currency { get; set; } = "USD";

        [Required(ErrorMessage = "Monthly base price is required.")]
        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? MonthlyBasePrice { get; set; }

        public string Interval { get; set; } = "Monthly";

        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? AnnualPrice { get; set; }

        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? OverageApiCalls { get; set; }

        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? OverageStorageBytes { get; set; }

        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? OverageUsers { get; set; }

        [Range(0, 1_000_000_000, ErrorMessage = "Must be a non-negative number.")]
        public decimal? OverageActiveFeatureFlags { get; set; }
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    [Parameter] public BillingPlanDto? Plan { get; set; }

    private MudForm? _mudForm;
    private readonly PlanForm _form = new();
    private bool _isSubmitting;

    private bool _isEdit => Plan is not null;

    private string? KeyValidator(string value)
    {
        if (_isEdit || KeyPattern.IsMatch(value))
        {
            return null;
        }

        return "Invalid slug — lowercase letters, digits, hyphens.";
    }

    protected override void OnInitialized()
    {
        if (Plan is null)
        {
            return;
        }

        _form.Key = Plan.Key;
        _form.Name = Plan.Name;
        _form.Currency = Plan.Currency;
        _form.MonthlyBasePrice = Plan.MonthlyBasePrice;
        _form.Interval = Plan.Interval == "Yearly" ? "Yearly" : "Monthly";
        _form.AnnualPrice = Plan.AnnualPrice;
        foreach (var (resource, rate) in Plan.OverageRates ?? new Dictionary<string, decimal>())
        {
            switch (resource)
            {
                case "ApiCalls": _form.OverageApiCalls = rate; break;
                case "StorageBytes": _form.OverageStorageBytes = rate; break;
                case "Users": _form.OverageUsers = rate; break;
                case "ActiveFeatureFlags": _form.OverageActiveFeatureFlags = rate; break;
            }
        }
    }

    private async Task SaveAsync()
    {
        await _mudForm!.ValidateAsync();
        if (!_mudForm.IsValid)
        {
            return;
        }

        _isSubmitting = true;
        try
        {
            var overage = new Dictionary<string, decimal>();
            AddRate(overage, "ApiCalls", _form.OverageApiCalls);
            AddRate(overage, "StorageBytes", _form.OverageStorageBytes);
            AddRate(overage, "Users", _form.OverageUsers);
            AddRate(overage, "ActiveFeatureFlags", _form.OverageActiveFeatureFlags);

            if (_isEdit && Plan is not null)
            {
                await BillingService.UpdatePlanAsync(Plan.Id, new UpdatePlanRequest(
                    _form.Name.Trim(),
                    _form.MonthlyBasePrice!.Value,
                    overage.Count == 0 ? null : overage,
                    _form.Interval,
                    _form.AnnualPrice));
            }
            else
            {
                await BillingService.CreatePlanAsync(new CreatePlanRequest(
                    _form.Key.Trim(),
                    _form.Name.Trim(),
                    _form.Currency.Trim().ToUpperInvariant(),
                    _form.MonthlyBasePrice!.Value,
                    overage.Count == 0 ? null : overage,
                    _form.Interval,
                    _form.AnnualPrice));
            }

            Dialog.Close(true);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to save plan: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private static void AddRate(Dictionary<string, decimal> overage, string resource, decimal? rate)
    {
        if (rate is > 0)
        {
            overage[resource] = rate.Value;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
