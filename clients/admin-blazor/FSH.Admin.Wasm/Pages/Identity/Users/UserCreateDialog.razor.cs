using System.ComponentModel.DataAnnotations;
using FSH.BlazorShared.Models.Identity;
using FSH.BlazorShared.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace FSH.Admin.Wasm.Pages.Identity.Users;

public sealed partial class UserCreateDialog
{
    public sealed class RegisterUserForm
    {
        [Required(ErrorMessage = "First name is required.")]
        [StringLength(64)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(64)]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Username is required.")]
        [RegularExpression(@"^[a-zA-Z][a-zA-Z0-9._-]{2,31}$", ErrorMessage = "Username must be 3-32 characters and start with a letter.")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [StringLength(32)]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm your password.")]
        [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    [CascadingParameter] private IMudDialogInstance Dialog { get; set; } = default!;
    [Inject] private IUserService UserService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;

    private MudForm? _mudForm;
    private readonly RegisterUserForm _form = new();
    private bool _isSubmitting;

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
            var request = new RegisterUserRequest(
                _form.FirstName,
                _form.LastName,
                _form.Email,
                _form.UserName,
                _form.Password,
                _form.ConfirmPassword,
                _form.PhoneNumber);
            var result = await UserService.CreateAsync(request);
            Snackbar.Add("User created. Confirmation email queued.", Severity.Success);
            Dialog.Close(result);
        }
        catch (Exception ex)
        {
            Snackbar.Add($"Failed to create user: {ex.Message}", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel() => Dialog.Cancel();
}
