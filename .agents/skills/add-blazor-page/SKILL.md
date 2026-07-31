---
name: add-blazor-page
description: Add a list+detail+create page to a Blazor WASM app (admin-blazor or dashboard-blazor) — service, page, route, permission gate, and tests. Use when adding any Blazor frontend screen. See .agents/rules/frontend/blazor-shared.md.
argument-hint: "[admin|dashboard] [Area] [Resource]"
---

# Add Blazor Page

Read `.agents/rules/frontend/blazor-shared.md` plus the app file (`blazor-admin.md` / `blazor-dashboard.md`).

Reference the React counterpart at `clients/{admin|dashboard}/src/pages/{area}/{page}.(tsx|ts)` for the UI pattern and API contract.

Key differences between apps:

| | **admin-blazor** | **dashboard-blazor** |
|---|---|---|
| Permission gating | `@attribute [Authorize(Policy=…)]` + `<FshPermissionGate>` | `@attribute [Authorize]` only (flatter model) |
| Forms | `MudForm` + DataAnnotations | `MudForm` or simpler controls |
| Loading | `MudSkeleton` / `MudProgressLinear` | `MudSkeleton` per-route |
| Nav registration | Permission-gated in `NavService` | Ungated in `NavService` |

## Step 1 — Service + DTOs (`BlazorShared/Services/` + `BlazorShared/Models/`)

Create the typed service interface and implementation:

```csharp
// BlazorShared/Models/{Module}/{Resource}Dto.cs
public record {Resource}Dto(string Id, string Name, /* … */);

public record Search{Resources}Request(int PageNumber = 1, int PageSize = 10, string? Search = null);

// BlazorShared/Services/I{Resource}Service.cs
public interface I{Resource}Service
{
    Task<PagedResult<{Resource}Dto>> SearchAsync(Search{Resources}Request request, CancellationToken ct = default);
    Task<{Resource}Dto> GetByIdAsync(string id, CancellationToken ct = default);
    Task CreateAsync(Create{Resource}Request request, CancellationToken ct = default);
    Task UpdateAsync(string id, Update{Resource}Request request, CancellationToken ct = default);
    Task DeleteAsync(string id, CancellationToken ct = default);
}

// BlazorShared/Services/{Resource}Service.cs
public sealed class {Resource}Service(HttpClient http) : I{Resource}Service
{
    private const string Base = "/api/v1/{module}/{resources}";

    public async Task<PagedResult<{Resource}Dto>> SearchAsync(Search{Resources}Request request, CancellationToken ct = default)
    {
        var q = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString(),
            ["Search"] = request.Search,
        };
        return await http.GetFromJsonAsync<PagedResult<{Resource}Dto>>($"{Base}/search?{ToQuery(q)}", ct)
               ?? new PagedResult<{Resource}Dto>([], 0, 0, 0, 0, false, false);
    }

    public async Task<{Resource}Dto> GetByIdAsync(string id, CancellationToken ct = default)
        => await http.GetFromJsonAsync<{Resource}Dto>($"{Base}/{id}", ct)
           ?? throw new InvalidOperationException("Null response");

    public async Task CreateAsync(Create{Resource}Request request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync(Base, request, ct);
        response.EnsureSuccessStatusCode();
    }
    // …
}
```

Register in DI (`Program.cs` of the consuming WASM app):
```csharp
builder.Services.AddHttpClient<I{Resource}Service, {Resource}Service>(client =>
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"] ?? "/"));
```

## Step 2 — List page (`Pages/{Area}/{Feature}ListPage.razor`)

```razor
@page "/{resources}"
@attribute [Authorize]  <!-- admin: add Policy="Permissions.{Resource}.View" -->
@inherits MudComponentBase

<FshPageHeader Icon="@Icons.Material.Filled.{Icon}"
               Title="{Resources}"
               Description="@($"Showing {_totalCount} {_resources}")">
    @if (CanCreate)
    {
        <MudButton Variant="Variant.Filled" Color="Color.Primary" @onclick="OpenCreateDialog">
            New {Resource}
        </MudButton>
    }
</FshPageHeader>

<FshFilterBar @bind-Search="_search" @bind-Page="_pageNumber" FilterChanged="() => _pageNumber = 1" />

@if (_isLoading)
{
    <MudSkeleton />
}
else if (_error is not null)
{
    <MudAlert Severity="Severity.Error">@_error</MudAlert>
}
else if (_items.Count == 0)
{
    <MudText>No {_resources} found.</MudText>
}
else
{
    <MudTable Items="@_items" Hover="true">
        <ColGroup>
            <col style="width: 60%" />
            <col style="width: 30%" />
            <col style="width: 10%" />
        </ColGroup>
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>Status</MudTh>
            <MudTh></MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="Name">
                <MudLink @onclick="() => NavigateTo(context.Id)">@context.Name</MudLink>
            </MudTd>
            <MudTd DataLabel="Status">
                <MudChip Color="GetStatusColor(context.Status)" Size="MudChipSize.Small">@context.Status</MudChip>
            </MudTd>
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Filled.MoreVert" Size="Size.Small" />
            </MudTd>
        </RowTemplate>
        <PagerContent>
            <MudTablePager PageSizeOptions="@(new[] { 10, 25, 50 })" />
        </PagerContent>
    </MudTable>
}
```

Code-behind (`{Feature}ListPage.razor.cs`):
```csharp
public sealed partial class {Resource}ListPage : MudComponentBase
{
    [Inject] private I{Resource}Service Service { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IAuthorizationService Auth { get; set; } = default!;

    private List<{Resource}Dto> _items = [];
    private int _totalCount;
    private int _pageNumber = 1;
    private string? _search;
    private bool _isLoading = true;
    private string? _error;
    private bool _canCreate;

    protected override async Task OnInitializedAsync()
    {
        _canCreate = (await Auth.AuthorizeAsync(null, "Permissions.{Resource}.Create")).Succeeded;
        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        _isLoading = true;
        _error = null;
        try
        {
            var result = await Service.SearchAsync(new Search{Resources}Request(_pageNumber, 10, _search));
            _items = result.Items;
            _totalCount = result.TotalCount;
        }
        catch (ApiRequestException ex) { _error = ex.Message; }
        finally { _isLoading = false; }
    }

    private void NavigateTo(string id) => Nav.NavigateTo($"/{resources}/{id}");
    private bool CanCreate => _canCreate;
    private void OpenCreateDialog() { /* or navigate */ }
    private Color GetStatusColor(string status) => status switch { "Active" => Color.Success, _ => Color.Default };
}
```

## Step 3 — Create/Edit (MudDialog)

```razor
@* In a shared Components/{Resource}EditDialog.razor *@
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@(_isEdit ? "Edit" : "New") {Resource}</MudText>
    </TitleContent>
    <DialogContent>
        <MudForm @ref="_form" Model="@_model">
            <MudTextField @bind-Value="_model.Name"
                          Label="Name"
                          For="@(() => _model.Name)"
                          Required="true" />
            <MudTextField @bind-Value="_model.Description"
                          Label="Description"
                          For="@(() => _model.Description)" />
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton OnClick="Submit" Variant="Variant.Filled" Color="Color.Primary"
                   Disabled="@_isSubmitting">
            @(_isEdit ? "Save" : "Create")
        </MudButton>
    </DialogActions>
</MudDialog>
```

## Step 4 — Route registration

Add `@page` directive at the top of each `.razor` file:
```razor
@page "/{resources}"
@page "/{resources}/{Id:guid}"
```

Admin-specific: add the policy to `Program.cs`:
```csharp
builder.Services.AddAuthorizationCore(options =>
{
    options.AddPolicy("Permissions.{Resource}.View",
        policy => policy.RequireClaim("permission", "Permissions.{Resource}.View"));
    // …
});
```

## Step 5 — (admin only) mirror the permission

Add constant to `BlazorShared/Permissions/`:
```csharp
public static class {Module}Permissions
{
    public static class {Resources}
    {
        public const string View = "Permissions.{Resources}.View";
        public const string Create = "Permissions.{Resources}.Create";
        public const string Edit = "Permissions.{Resources}.Edit";
        public const string Delete = "Permissions.{Resources}.Delete";
    }
}
```

## Step 6 — bUnit test

```csharp
public class {Resource}ListPageTests : TestContext
{
    [Fact]
    public void Renders_loading_state()
    {
        // Arrange
        Services.AddMockHttpClient<I{Resource}Service, {Resource}Service>(handler =>
            handler.When(HttpMethod.Get, "/api/v1/{module}/{resources}/search")
                   .Respond(/* delayed */));

        // Act
        var cut = RenderComponent<{Resource}ListPage>();

        // Assert
        cut.FindComponents<MudSkeleton>().ShouldNotBeEmpty();
    }
}
```

## Step 7 — Playwright test

```typescript
test.beforeEach(async ({ page }) => {
    // admin: seedAuthedSession(page, { ...TEST_USER, permissions: [...ADMIN_PERMS] }); await installAdminShellMocks(page);
    // dashboard: await seedAuthedSession(page, TEST_USER); await installShellMocks(page);
    await mockJsonResponse(page, `**/api/v1/{module}/{resources}/search**`, paged([SAMPLE]));
});

test("renders list", async ({ page }) => {
    await page.goto(`/{resources}`);
    await expect(page.getByRole("table")).toBeVisible();
});
```

## Checklist

- [ ] Service: `I{Name}Service` + `{Name}Service` with typed methods
- [ ] Models: DTOs as C# records in `BlazorShared/Models/{Module}/`
- [ ] List page: `MudTable` with search, filter, pagination, loading/empty/error states
- [ ] Create/Edit: `MudForm` with `MudDialog`
- [ ] Detail page: `MudCard` sections
- [ ] Route: `@page` directives registered
- [ ] (admin) Permission constant mirrored + policy registered
- [ ] (admin) Route-level `@attribute [Authorize(Policy="...")]`
- [ ] DI registration in `Program.cs`: `AddHttpClient`
- [ ] bUnit test: loading, empty, error, data states
- [ ] Playwright test: seed + shell mocks + page mocks
