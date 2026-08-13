using FSH.BlazorShared.Models.Dashboard;
using FSH.BlazorShared.Services;

namespace FSH.Hybrid.Pages;

public sealed partial class OverviewPage
{
    private TenantStatusDto? _status;
    private bool _loading = true;
    private string? _error;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _status = await Dashboard.GetMyTenantStatusAsync();
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
}
