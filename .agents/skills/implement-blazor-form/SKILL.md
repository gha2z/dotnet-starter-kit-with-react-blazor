---
name: implement-blazor-form
description: Create a MudForm with validation for create/edit operations. Use when adding a form to a Blazor WASM page. Follows the admin app pattern (admin uses forms; dashboard is lighter).
argument-hint: "[admin|dashboard] [Resource]"
---

# Implement Blazor Form

Read `.agents/rules/frontend/blazor-shared.md` and `blazor-admin.md` (if admin).

Companion skill: `collect-user-input` (generic Blazor forms/validation) — this skill is the
MudForm + DataAnnotations variant specific to FSH.

## Step 1 — Model with data annotations

```csharp
public class Create{Resource}Model
{
    [Required(ErrorMessage = "Name is required.")]
    [StringLength(64, ErrorMessage = "Name must be at most 64 characters.")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
    public string Password { get; set; } = string.Empty;

    [Required]
    [Compare("Password", ErrorMessage = "Passwords don't match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

For cross-field validation, implement `IValidatableObject`.

## Step 2 — MudForm with validation

```razor
<MudForm @ref="_form"
         Model="@_model"
         Validation="@(new DataAnnotationsValidator())"
         @onsubmit="SubmitAsync">
    <MudTextField @bind-Value="_model.Name"
                  Label="Name"
                  For="@(() => _model.Name)"
                  Required="true"
                  RequiredError="Name is required." />
    <MudTextField @bind-Value="_model.Email"
                  Label="Email"
                  For="@(() => _model.Email)"
                  Required="true"
                  HelperText="Enter a valid email address." />
    <MudPasswordField @bind-Value="_model.Password"
                      Label="Password"
                      For="@(() => _model.Password)"
                      Required="true" />
    <MudPasswordField @bind-Value="_model.ConfirmPassword"
                      Label="Confirm Password"
                      For="@(() => _model.ConfirmPassword)"
                      Required="true" />
    <MudButton ButtonType="ButtonType.Submit"
               Variant="Variant.Filled"
               Color="Color.Primary"
               Disabled="@_isSubmitting"
               FullWidth="true">
        @(_isEdit ? "Update" : "Create")
    </MudButton>
</MudForm>
```

## Step 3 — Submit handler

```csharp
private async Task SubmitAsync()
{
    await _form.Validate();
    if (!_form.IsValid) return;

    _isSubmitting = true;
    try
    {
        if (_isEdit)
            await _service.UpdateAsync(_id!, _model);
        else
            await _service.CreateAsync(_model);

        Snackbar.Add(_isEdit ? "Updated successfully." : "Created successfully.", Severity.Success);
        MudDialog.Close(DialogResult.Ok(true));
    }
    catch (ApiRequestException ex)
    {
        Snackbar.Add(ex.Message, Severity.Error);
    }
    finally
    {
        _isSubmitting = false;
    }
}
```

## Validation

- [ ] Form displays validation errors on submit with invalid data
- [ ] Successful submit calls service method and shows success toast
- [ ] Error from API shown as error toast
- [ ] Submit button disabled while submitting
