using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using FSH.BlazorShared.Models.Billing;
using FSH.BlazorShared.Models.Tenants;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Tenants;

public sealed partial class TenantCreateDialog
{
    private const string TenantIdRegex = "^[a-z0-9][a-z0-9-]{1,62}[a-z0-9]$";
    private const string PasswordCharset = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789!@#$%^&*?";

    public sealed class TenantForm
    {
        [Required(ErrorMessage = "Display name is required.")]
        [StringLength(128, MinimumLength = 2, ErrorMessage = "Name must be 2-128 characters.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Identifier is required.")]
        [RegularExpression(TenantIdRegex, ErrorMessage = "Lowercase letters, digits, hyphens. 3-64 chars. No leading/trailing hyphen.")]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email.")]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "At least 8 characters.")]
        public string AdminPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issuer is required.")]
        [StringLength(256, MinimumLength = 2, ErrorMessage = "Issuer must be 2-256 characters.")]
        public string Issuer { get; set; } = string.Empty;

        [StringLength(2048, ErrorMessage = "Keep under 2048 characters.")]
        public string? ConnectionString { get; set; }

        public string? PlanKey { get; set; }
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private ITenantService TenantService { get; set; } = default!;
    [Inject] private IBillingService BillingService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudForm? _mudForm;
    private readonly TenantForm _form = new();
    private bool _isSubmitting;
    private bool _idAutoMode = true;
    private bool _issuerDirty;
    private List<BillingPlanDto> _plans = [];
    private bool _plansError;

    private string IdHelperText => _idAutoMode
        ? "Lowercase letters, digits, and hyphens. Auto-derived from the display name — click Edit to change."
        : "Unlocked — you're editing it by hand.";

    private string PlanHelperText => _plansError
        ? "Could not load plans — the tenant will fall back to the default plan."
        : "Sets the first invoice and how long the tenant stays valid. Defaults to the trial plan.";

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _plans = await BillingService.GetPlansAsync(includeInactive: false);
            if (_plans.Count > 0 && string.IsNullOrEmpty(_form.PlanKey))
            {
                _form.PlanKey = _plans.FirstOrDefault(p => p.Key == "free")?.Key ?? _plans[0].Key;
            }
        }
        catch (Exception)
        {
            _plansError = true;
        }
    }

    private Task OnNameChangedAsync(string value)
    {
        _form.Name = value;
        if (_idAutoMode)
        {
            _form.Id = Slugify(value);
            if (!_issuerDirty)
            {
                _form.Issuer = _form.Id;
            }
        }

        return Task.CompletedTask;
    }

    private Task OnIdChangedAsync(string value)
    {
        _form.Id = value;
        if (!_issuerDirty)
        {
            _form.Issuer = value;
        }

        return Task.CompletedTask;
    }

    private Task OnAdminEmailChangedAsync(string value)
    {
        _form.AdminEmail = value;
        return Task.CompletedTask;
    }

    private Task OnAdminPasswordChangedAsync(string value)
    {
        _form.AdminPassword = value;
        return Task.CompletedTask;
    }

    private Task OnIssuerChangedAsync(string value)
    {
        _form.Issuer = value;
        _issuerDirty = true;
        return Task.CompletedTask;
    }

    private Task OnConnectionStringChangedAsync(string value)
    {
        _form.ConnectionString = value;
        return Task.CompletedTask;
    }

    private Task OnPlanKeyChangedAsync(string value)
    {
        _form.PlanKey = value;
        return Task.CompletedTask;
    }

    private void ToggleIdMode()
    {
        if (_idAutoMode)
        {
            _idAutoMode = false;
        }
        else
        {
            _idAutoMode = true;
            _form.Id = Slugify(_form.Name);
            if (!_issuerDirty)
            {
                _form.Issuer = _form.Id;
            }
        }
    }

    private void GeneratePassword()
    {
        var bytes = new byte[16];
        Random.Shared.NextBytes(bytes);
        var chars = new char[16];
        for (var i = 0; i < chars.Length; i++)
        {
            chars[i] = PasswordCharset[bytes[i] % PasswordCharset.Length];
        }

        _form.AdminPassword = new string(chars);
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
            var request = new CreateTenantRequest(
                _form.Id,
                _form.Name,
                string.IsNullOrWhiteSpace(_form.ConnectionString) ? null : _form.ConnectionString,
                _form.AdminEmail,
                _form.AdminPassword,
                _form.Issuer,
                string.IsNullOrWhiteSpace(_form.PlanKey) ? null : _form.PlanKey);
            var result = await TenantService.CreateAsync(request);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create tenant: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();

    private static string Slugify(string input)
    {
        var slug = Regex.Replace(input.ToLowerInvariant().Trim(), "[^a-z0-9]+", "-").Trim('-');
        return slug.Length > 64 ? slug[..64] : slug;
    }

    internal static string PlanLabel(BillingPlanDto plan)
    {
        var interval = plan.Interval == "Yearly" ? "Yearly" : "Monthly";
        var amount = plan.Interval == "Yearly" && plan.AnnualPrice is { } annual ? annual : plan.MonthlyBasePrice;
        return $"{plan.Name} · {interval} · {plan.Currency} {amount:F2}";
    }
}
