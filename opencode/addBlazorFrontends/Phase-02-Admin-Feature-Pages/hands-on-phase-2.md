# Chapter 2: Admin Feature Pages — The Users Feature

> **Audience**: From absolute beginner to senior. Each section has tiered callouts.
> **Prerequisites**: Chapter 1 (Identity & Auth) complete. You can log in to `FSH.Admin.Wasm`.
> **Time to complete**: 4-6 hours reading + 3-4 hours hands-on.
> **What you'll know after this chapter**: How the client-side data layer (DTOs + services) mirrors the API, how `MudTable` server-mode pagination works, how to build a MudForm dialog, how to render multi-section detail pages, and how to test all of it with bUnit — including every MudBlazor 9 testing trap we hit.

---

## 2.0 The Big Picture — From API to Screen

Everything in this chapter is one pipeline: **HTTP JSON → DTO → service → component → markup**.

```
┌────────────┐   GET /api/users?PageNumber=1&PageSize=12&SortBy=firstname
│  FSH API   │ ◀───────────────────────────────────────────┐
│ (backend)  │                                             │
└─────┬──────┘                                             │
      │ 200 OK: { items: [ {id, userName, ...} ], totalCount: 2 }
      ▼                                                     │
┌────────────┐   deserializes JSON into records             │
│  DTOs      │   UserDto, PagedResult<UserDto>,             │
│ (models)   │   SearchRequest, RegisterUserRequest         │
└─────┬──────┘                                             │
      ▼                                                     │
┌────────────┐   builds URL query strings,                  │
│  Services  │   calls HttpClient, returns DTOs             │
│  (IUserService)                                       │
└─────┬──────┘                                             │
      ▼                                                     │
┌────────────┐   calls the service, keeps UI state,         │
│  Pages     │   renders MudTable rows / MudForm fields     │
└────────────┘                                             │
      ▲                                                     │
      └────── user types, clicks, toggles ──────────────────┘
```

**The golden rule of this pipeline:** each layer only knows about the layer next to it.
- Pages never see `HttpClient` or JSON.
- Services never see `MudTable` or `MudForm`.
- DTOs never do anything — they're dumb data carriers.

> **🐣 Beginner**: Think of the DTO like a paper form (pre-printed fields), the service like a mail clerk (delivers the form, brings back the reply), and the page like the manager (reads the reply, decides what to display).

> **👨‍🔬 Senior Note**: This layering is what makes the feature *testable*. The page test substitutes `IUserService` with NSubstitute — no HTTP, no backend, no Docker. If the layering is leaky (page builds its own query strings), the test surface explodes. The service layer is your unit-testable seam.

### The React Comparison

You already have `clients/admin` (React). This chapter builds the same three screens in Blazor:

| Screen | React file | Blazor file |
|--------|-----------|-------------|
| List | `src/pages/users/list.tsx` | `Pages/Identity/Users/UsersListPage.razor` |
| Create dialog | `src/components/users/create-user-dialog.tsx` | `Pages/Identity/Users/UserCreateDialog.razor` |
| Detail | `src/pages/users/detail.tsx` | `Pages/Identity/Users/UserDetailPage.razor` |
| API layer | `src/api/users.ts` | `Services/UserService.cs` |

Keep both open while reading — parity is the point.

---

## 2.1 The DTOs — Dumb Data Carriers

File: `clients/BlazorShared/Models/Identity/UserDtos.cs`

```csharp
public sealed record UserDto(
    string? Id,
    string? UserName,
    string? FirstName,
    string? LastName,
    string? Email,
    bool IsActive,
    bool EmailConfirmed,
    string? PhoneNumber,
    string? ImageUrl,
    bool IsOnline);
```

And the paging envelope the API returns:

```csharp
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int PageCount,
    bool HasPrevious,
    bool HasNext);
```

### Why records?

- **Value semantics**: two DTOs with the same data are `==` equal — handy in tests.
- **Positional constructor**: the property list *is* the constructor — no boilerplate.
- **Immutable**: a DTO you receive can't be mutated by accident; changes go through a command/request DTO.

> **🐣 Beginner**: A `record` is C#'s shorthand for "a class whose whole job is carrying data." `record Person(string Name, int Age)` is a complete class — constructor, properties, equality, `ToString()` — in one line.

> **👨‍🔬 Senior Note**: The backend's contract DTOs (`src/Modules/Identity/Modules.Identity.Contracts/DTOs/`) are the source of truth for field names. When the API adds a field, the Blazor DTO changes here — and **tests** catch mismatches at the boundary (see 2.6). JSON names: the API uses camelCase (`userName`), and `UserService`'s `HttpClient` JSON options handle the case conversion — the C# property is `UserName`, the JSON key is `userName`.

### Why not reuse the backend contracts?

The contracts live in `src/Modules/...Contracts` — backend-only, referenced by the API host. The Blazor WASM app cannot reference them (they'd drag in server assemblies; WASM runs in the browser). So each client mirrors the shape it needs. This is normal and healthy — the DTO is the *contract at the HTTP boundary*.

---

## 2.2 The Services — Talking to the API

File: `clients/BlazorShared/Services/UserService.cs` (plus `IUserService.cs`)

```csharp
public sealed class UserService(HttpClient http) : IUserService
{
    public async Task<PagedResult<UserDto>> SearchAsync(SearchRequest request, CancellationToken ct = default)
    {
        var query = QueryStringBuilder.Build(request);
        var response = await http.GetAsync($"api/users?{query}", ct).ConfigureAwait(false);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>(ct).ConfigureAwait(false);
        return result ?? new PagedResult<UserDto>([], 0, request.PageSize, 0, 0, false, false);
    }
}
```

And the query-string builder lives with `SearchRequest`:

```csharp
public sealed record SearchRequest(
    int PageNumber = 1,
    int PageSize = 10,
    string? Search = null,
    string? SortBy = null,
    string? SortDirection = null,
    Dictionary<string, string?>? Filters = null);
```

### Three service-layer lessons

1. **URL building is the service's job.** The page says "I want page 2, sorted by name, filtered by status" — it never touches a `?` character. This keeps every layer independently testable and keeps the page readable.

2. **`ConfigureAwait(false)` on every await.** The service has no UI thread to return to — it runs on a thread-pool thread once awaited from a background continuation. This is a continuation of the repo's golden rule #5.

3. **Null-safe deserialization.** `ReadFromJsonAsync` returns `null` when the body is empty. Return a typed fallback so no `null` can ever escape the boundary — the page's `LoadDataAsync` never has to `?.`.

> **🐣 Beginner**: `HttpClient` here is a **singleton injected by DI** — see `Program.cs` where `AddScoped(sp => new HttpClient { BaseAddress = ... })` registers it. The base address comes from the runtime config (`/config.json`), not a hardcoded URL.

> **👨‍🔬 Senior Note**: The JSON deserialization uses the SAME `JsonSerializerOptions` the rest of the client uses (case-insensitive, camelCase). Keep it in one place — two sets of options = two debugging sessions.

---

## 2.3 The List Page — Server-Mode MudTable

File: `Pages/Identity/Users/UsersListPage.razor` (+ `.razor.cs`)

### What "server mode" means

A *client-side* table holds all rows in memory and filters them in the browser. A *server-mode* table (`ServerData="LoadDataAsync"`) asks **your code** for rows each time the user pages, sorts, or changes filters:

```csharp
private async Task<TableData<UserDto>> LoadDataAsync(TableState state, CancellationToken ct)
{
    var request = new SearchRequest(
        PageNumber: state.Page + 1,          // MudTable is 0-based, API is 1-based
        PageSize: state.PageSize,
        Search: _searchInput,
        SortBy: state.SortLabel,             // "firstname" | "username" | ...
        SortDirection: state.SortDirection == SortDirection.Descending ? "desc" : "asc",
        Filters: new Dictionary<string, string?>
        {
            ["IsActive"] = _activeFilter switch { Tri.Yes => "true", Tri.No => "false", _ => null },
            ["EmailConfirmed"] = _confirmedFilter switch { Tri.Yes => "true", Tri.No => "false", _ => null },
            ["RoleId"] = string.IsNullOrEmpty(_roleId) ? null : _roleId,
        });

    var result = await UserService.SearchAsync(request, ct);
    _totalCount = result.TotalCount;
    _error = null;
    await InvokeAsync(StateHasChanged);      // see "The bug the tests found" below
    return new TableData<UserDto> { Items = result.Items, TotalItems = result.TotalCount };
}
```

The signature is fixed: `Func<TableState, CancellationToken, Task<TableData<T>>>`. MudTable calls it:
- once on first render,
- again when the user clicks a sort label, page, or `ReloadServerData()` is called,
- **cancels the in-flight call** with the `CancellationToken` when a new one starts.

### The two things that look wrong but are right

1. **`state.Page + 1`** — MudTable pages from 0; the API pages from 1. Off-by-one bugs in pagination are silent: the last page shows empty. Test it.

2. **`state.SortLabel`** — in MudBlazor 9 you put a *sort key* on the header, not a lambda:
   ```razor
   <MudTh><MudTableSortLabel T="UserDto" SortLabel="firstname">Name</MudTableSortLabel></MudTh>
   ```
   - `SortBy="..."` on the label expects a `Func<T, object>` (client-side sorting). For server mode you want `SortLabel="..."` — a string key that lands verbatim in `state.SortLabel`.
   - Putting `SortLabel` on `<MudTh>` directly was the v8 way — v9 throws a `MUD0002` warning (illegal attribute). The label component owns it now.

### Debounced search

```csharp
private async Task OnSearchChangedAsync(string value)
{
    _searchInput = value;
    _debounceCts?.Cancel();
    _debounceCts = new CancellationTokenSource();
    try
    {
        await Task.Delay(250, _debounceCts.Token);
        await ReloadAsync();
    }
    catch (TaskCanceledException) { }   // a newer keystroke cancelled this one
}
```

`Task.Delay(250, token)` + cancel-on-keystroke = every keystroke resets the timer; only a 250ms pause triggers the reload. The `catch` swallows the *expected* cancellation — not an error.

> **🐣 Beginner**: "Debounce" = wait until the user *stops* typing before acting. Without it, every keystroke fires a server request; with it, a fast typist fires one.

### Filter selects

Filters use `MudSelect` with a **tri-state string mapping**:

```razor
<MudSelect T="string" Value="@ActiveFilterValue" ValueChanged="@OnActiveFilterChanged" Label="Status" Clearable="true" ...>
    <MudSelectItem Value="@ActiveOption">Active</MudSelectItem>
    <MudSelectItem Value="@DisabledOption">Disabled</MudSelectItem>
</MudSelect>
```

with constants in the code-behind:

```csharp
private const string ActiveOption = "Active";
private const string DisabledOption = "Disabled";
```

Why constants? **Razor's bare-word value `Value="Active"` is parsed as a C# expression** — and `Active` isn't a field, so it would bind against a component parameter of the same name or fail. The constant makes the intent explicit and keeps the string in one place (the enum `Tri { Any, Yes, No }` is the real state; the string is only the UI transport).

> **👨‍🔬 Senior Note**: `Clearable="true"` makes MudSelect emit `null` when cleared — so `OnActiveFilterChanged(null)` maps to `Tri.Any`, and the filter dictionary gets `null`, which the service layer omits from the query string. `null` in a `Dictionary<string, string?>` = "this filter is off" — never delete the key, always write `null`.

### The bug the tests found

The page header shows "N accounts on this tenant." — a summary line that depends on `_totalCount`. Where does `_totalCount` get set? Inside `LoadDataAsync` — **a callback invoked by MudTable, not by the page's own lifecycle**. MudTable calls `StateHasChanged()` on *itself* after `ServerData` completes, so the rows appear — but the *page* never re-rendered, so the header stayed on "Loading the roster…".

Fix: after mutating page state in the callback, tell the page to re-render:

```csharp
await InvokeAsync(StateHasChanged);
```

> **🐣 Beginner**: `StateHasChanged` = "please render me again." Blazor components re-render when *their own* events/lifecycle complete — a callback from a *child* (here MudTable) doesn't automatically re-render the parent.
>
> **👨‍🔬 Senior Note**: This is a classic "callback writes shared state" bug. `LoadDataAsync` runs inside MudTable's rendering pipeline; any state it mutates *outside the returned `TableData`* needs an explicit re-render. The alternative — lifting the count into the `TableData` — isn't possible; the count *is* in `TableData.TotalItems`, and the summary line is page-level presentation. Write a test for this exact case (2.6.3); it's the only thing that catches it.

### Row click navigation

```razor
<MudTable @ref="_table" ServerData="LoadDataAsync" OnRowClick="@((TableRowClickEventArgs<UserDto> e) => RowClickAsync(e))" ...>
```

and

```csharp
private async Task RowClickAsync(TableRowClickEventArgs<UserDto> args)
{
    if (args.Item?.Id is { } id)
    {
        Nav.NavigateTo($"/users/{id}");
    }
}
```

Why the lambda and not a method group (`OnRowClick="RowClickAsync"`)? In MudBlazor 9, `OnRowClick` is `EventCallback<TableRowClickEventArgs<T>>` — the method group's parameter type is checked against the callback's; the lambda pins the generic type argument so the compiler binds it cleanly (a method group produces CS1503 here). The lambda is the idiomatic v9 form.

---

## 2.4 The Create Dialog — MudForm Inside MudDialog

File: `Pages/Identity/Users/UserCreateDialog.razor`

### Opening the dialog

```csharp
var options = new DialogOptions { MaxWidth = MaxWidth.Small, FullWidth = true, CloseButton = true };
var dialog = await DialogService.ShowAsync<UserCreateDialog>("New user", options);
var result = await dialog.Result;
if (result is not null && !result.Canceled && result.Data is RegisterUserResponse response && response.UserId is not null)
{
    Nav.NavigateTo($"/users/{response.UserId}");   // React parity: create → land on detail
}
```

`ShowAsync<T>` (the v9 API — `Show<T>` is the legacy overload) returns an `IDialogReference` whose `Result` task completes when the dialog closes with `DialogResult.Ok(data)` or `DialogResult.Cancel()`.

### Inside the dialog

```razor
<MudForm @ref="_form" Validation="@(new RegisterUserValidator())">
    <MudTextField T="string" Label="First name" @bind-Value="_request.FirstName" For="@(() => _request.FirstName)" />
    ...
    <MudTextField T="string" Label="Password" @bind-Value="_request.Password" InputType="InputType.Password" />
    <MudTextField T="string" Label="Confirm password" @bind-Value="_request.ConfirmPassword" InputType="InputType.Password" />
</MudForm>
```

Validation is FluentValidation on the request type — the same rules style as the backend, now enforced in the browser:

```csharp
public sealed class RegisterUserValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().WithMessage("First name is required.");
        RuleFor(x => x.LastName).NotEmpty().WithMessage("Last name is required.");
        RuleFor(x => x.UserName).NotEmpty().WithMessage("Username is required.").MinimumLength(3);
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required.").EmailAddress();
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.").MinimumLength(6);
        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm your password.")
            .Equal(x => x.Password).WithMessage("Passwords do not match.");
    }
}
```

On submit:

```csharp
private async Task SubmitAsync()
{
    await _form.Validate();
    if (!_form.IsValid) return;

    try
    {
        var response = await UserService.CreateAsync(_request);
        DialogService.Close(DialogResult.Ok(response));
    }
    catch (Exception ex)
    {
        Snackbar.Add($"Could not create user: {ex.Message}", Severity.Error);
    }
}
```

Note the two-step guard: **validation first, then the call**. The dialog closes *only* with `Ok`; on failure it stays open with a snackbar — the test suite pins this behavior (2.6.5).

> **🐣 Beginner**: `@bind-Value` is two-way binding: `Value` + `ValueChanged` wired together. The dialog edits a mutable request object; the DTOs from 2.1 stay immutable — the request is the *input*, the DTO is the *output*.
>
> **👨‍🔬 Senior Note**: `DialogService.Close(DialogResult.Ok(response))` — pass the created record out through the result. The caller (`OpenCreateDialogAsync`) then navigates to the new user without a second fetch. React does the same (`navigate(`/users/${user.id}`)` after `createUser`).

---

## 2.5 The Detail Page — Cards, Toggles, Pending Changes

File: `Pages/Identity/Users/UserDetailPage.razor`

Three sections, mirroring the React `detail.tsx`:

1. **Identity card** — avatar, display name, username/email, phone, tenant, created-at.
2. **Roles card** — one `MudSwitch T="bool"` per role; **nothing is saved until the user clicks "Save changes"** (a dirty-check count "N pending change(s)" drives the button's enabled state and the badge).
3. **Sessions card** — a small table of active sessions (browser, IP, last-active) from `GetSessionsAsync(userId)`.

### The pending-changes pattern

```csharp
private readonly Dictionary<string, bool> _roleAssignments = [];

private async Task OnRoleToggledAsync(string roleId, bool checkedState)
{
    _roleAssignments[roleId] = checkedState;
    _pendingCount = _roleAssignments.Count;          // or diff against server state
    await InvokeAsync(StateHasChanged);
}
```

Save:

```csharp
await UserService.AssignRolesAsync(_userId, _roleAssignments);
_pendingCount = 0;
Snackbar.Add("Roles updated", Severity.Success);
```

Why not save on toggle? The React app batches: toggle → "pending" badge → one PUT. It's better UX (no per-toggle spinner) and matches the reference exactly.

> **🐣 Beginner**: `MudSwitch` in v9 is generic: `MudSwitch T="bool"` — the non-generic form is gone. `@bind-Value="..."; OnChange="..."` runs your handler after the bound value updates.
>
> **👨‍🔬 Senior Note**: In tests, `MudSwitch` renders `<input type="checkbox" class="mud-switch-input" @onchange=...>` — so you must drive it with `.Change(bool)`, **not** `.Click()`. Click dispatches `onclick`; the handler listens on `onchange`. This is the single most common MudBlazor test mistake.

### The status toggle

```razor
<MudSwitch T="bool" @bind-Value="_user.IsActive" OnChange="OnStatusChangedAsync" ...>
```

with a confirm-against-conflict flow: if the user has active sessions, show a dialog asking to revoke them (parity with React's deactivate → force-logout flow).

---

## 2.6 Testing with bUnit — Everything We Learned the Hard Way

File: `FSH.Admin.Wasm.Tests/` — xUnit + bUnit 2.0.66 + NSubstitute + Shouldly.

### 2.6.1 The TestSetup base — three bUnit 2 traps

```csharp
public class TestSetup : BunitContext, IAsyncLifetime
{
    public TestSetup()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;      // Trap 1
        AddAuthorization();                         // Trap 2
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => ((IAsyncDisposable)this).DisposeAsync().AsTask();  // Trap 3
}
```

| Trap | Symptom | Fix |
|------|---------|-----|
| **1. JS interop** | MudBlazor components call `mudKeyInterceptor`, focus, etc. — without a browser every call throws | `JSInterop.Mode = JSRuntimeMode.Loose` returns default values instead of invoking JS |
| **2. Authorization** | Pages have `[Authorize(Policy = ...)]`; without auth services, every render throws `InvalidOperationException: no authentication handler` | `AddAuthorization()` — a method **on `BunitContext`**, not on `Services` (the old extension `AddAuthorizationCore()` is gone in bUnit 2) |
| **3. Async disposal** | MudBlazor 9 services implement only `IAsyncDisposable`; bUnit's synchronous `Dispose()` throws `InvalidOperationException: 'MudBlazor.KeyInterceptorService' type only implements IAsyncDisposable` | Inherit `IAsyncLifetime` and forward `DisposeAsync` explicitly (needs `using Xunit;` for the interface) |

### 2.6.2 Rendering and waiting

- `Render<T>()` is the bUnit 2 name — `RenderComponent<T>()` is obsolete (warning CS0618).
- NSubstitute stubs return `Task`s; the component's `OnInitializedAsync` awaits them; rows appear **one render later** than the stub call returns. So:

```csharp
var cut = Render<UsersListPage>();
cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jane Doe"));
```

`WaitForAssertion` retries until the assertion passes or the timeout — use it for anything that happens *after* an await. Plain `ShouldContain` on the first snapshot fails even when the page is perfectly fine.

> **🐣 Beginner**: `Render<T>()` builds the component with services registered on `Services` (`Services.AddSingleton(_userService)`), runs `OnInitializedAsync`, and hands you a `cut` object whose `.Markup` is the rendered HTML. That HTML is what the browser *would* show.

### 2.6.3 The test that caught the real page bug

```csharp
[Fact]
public void Renders_users_and_roles_loaded_from_services()
{
    _roleService.ListAsync(Arg.Any<CancellationToken>())
        .Returns([SampleRole("r1", "Admin"), SampleRole("r2", "User")]);
    _userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
        .Returns(new PagedResult<UserDto>([...2 users...], 1, 12, 2, 1, false, false));

    var cut = Render<UsersListPage>();

    cut.WaitForAssertion(() => cut.Markup.ShouldContain("Jane Doe"));
    cut.WaitForAssertion(() =>
    {
        cut.Markup.ShouldContain("@john");
        cut.Markup.ShouldContain("jane@example.com");
        cut.Markup.ShouldContain("2 accounts on this tenant.");   // ← failed before InvokeAsync fix
    });
}
```

"2 accounts on this tenant." never appeared until we added `InvokeAsync(StateHasChanged)` in `LoadDataAsync` (2.3). **This is a real production bug that only the test caught** — that's what the test is for.

### 2.6.4 Selectors — three MudBlazor 9 gotchas

1. **The header row is a `tr.mud-table-row` too.** `cut.FindAll("tr.mud-table-row").Count.ShouldBe(1)` fails with count 2 (header + one data row). Scope to the body: `cut.FindAll("tbody tr.mud-table-row")`.
2. **Popover content doesn't render without `MudPopoverProvider`.** A `MudSelect`'s items live in a popover. Rendered standalone, the popover div is empty — items only appear if `MudPopoverProvider` is in the same renderer:
   ```csharp
   var cut = Render(builder =>
   {
       builder.OpenComponent<MudPopoverProvider>(0);
       builder.CloseComponent();
       builder.OpenComponent<UsersListPage>(1);
       builder.CloseComponent();
   });
   ```
   (v9 `MudPopoverProvider` has no `ChildContent`, so siblings, not nesting.)
3. **`MudSelect` opens on `onmousedown`, not `onchange`/`onclick`.** Trigger it:
   ```csharp
   cut.FindAll("div.mud-input-control.mud-select")[0]
       .TriggerEvent("onmousedown", new MouseEventArgs());
   ```
   and note `cut.Find(".mud-input-control")` matches the *search text field* first — always scope with `.mud-select`.

### 2.6.5 The dialog tests — render through a real provider

`MudDialog` in v9 renders its content **only when a real dialog instance exists** (it checks a *private* `IMudDialogInstanceInternal` cascade — a mocked `IMudDialogInstance` does not satisfy it). The reliable pattern is the real thing:

```csharp
private async Task<(IRenderedComponent<MudDialogProvider>, IDialogReference)> ShowDialogAsync()
{
    var provider = Render<MudDialogProvider>();
    var dialogService = Services.GetRequiredService<IDialogService>();
    var reference = await dialogService.ShowAsync<UserCreateDialog>("New user");
    provider.WaitForAssertion(() => provider.FindAll("input").Count.ShouldBe(7));
    return (provider, reference);
}
```

Then the tests exercise the *full* dialog flow — open, fill, validate, call the service, close:

```csharp
[Fact]
public async Task Valid_form_creates_user_and_closes_dialog()
{
    _userService.CreateAsync(Arg.Any<RegisterUserRequest>(), Arg.Any<CancellationToken>())
        .Returns(new RegisterUserResponse("u1", "User Created"));
    var (provider, reference) = await ShowDialogAsync();

    FillValidForm(provider.FindAll("input"));          // .Change(...) on the 7 inputs
    provider.FindAll("button").First(b => b.TextContent.Contains("Create user")).Click();

    await _userService.Received(1).CreateAsync(Arg.Is<RegisterUserRequest>(r =>
        r.FirstName == "Jane" && ... && r.Password == "Str0ng!Pass"), Arg.Any<CancellationToken>());

    var result = await reference.Result;               // dialog closed → result completed
    result!.Canceled.ShouldBeFalse();
    result.Data.ShouldBeOfType<RegisterUserResponse>().UserId.ShouldBe("u1");
}
```

### 2.6.6 NSubstitute expression-tree limits

Inside `Arg.Is<T>(...)`, NSubstitute compiles your expression into a delegate — **C# pattern syntax is not allowed**:

```csharp
// ❌ Arg.Is<UserDto>(u => u is { UserName: "jane" })
// ❌ Arg.Is<string>(s => s is not null)
// ✅
Arg.Is<UserDto>(u => u != null && u.UserName == "jane")
```

Use explicit comparisons (`==`, `!=`, casts). The compiler errors are cryptic (CS0149/CS0834) — remember this one and save the debugging session.

### 2.6.7 What the 14 tests cover

| Test file | Count | Covers |
|-----------|-------|--------|
| `UsersListPageTests` | 6 | render + count header, row-click navigation, debounced search (asserts the *request*, 5s timeout), filter change → reload with filter dict, role-load failure alert, user-load failure alert |
| `UserCreateDialogTests` | 4 | empty-form validation blocks, valid form creates + closes + returns result, password mismatch blocks, service failure keeps dialog open |
| `UserDetailPageTests` | 4 | renders identity/roles/sessions, role toggle → pending count → save assigns roles, status toggle deactivates, load failure alert |

### 2.6.8 The mock failure trap

A test that forgets to stub `SearchAsync` gets `null` from NSubstitute — and `await null` throws `NullReferenceException` *inside the page*, where the catch block formats it into the *user* error alert. Symptom: the role-failure test shows "Failed to load users: Object reference not set…". Fix in the test: stub **every** service the page touches, even the ones the test doesn't care about:

```csharp
_userService.SearchAsync(Arg.Any<SearchRequest>(), Arg.Any<CancellationToken>())
    .Returns(new PagedResult<UserDto>([], 1, 12, 0, 0, false, false));  // harmless default
```

This also surfaced a real design fix: role-load errors and user-load errors are **separate states** (`_roleError` / `_error`) — one alert shouldn't overwrite the other.

---

## 2.7 Running the Suite

```bash
dotnet test clients/admin-blazor/FSH.Admin.Wasm.Tests/FSH.Admin.Wasm.Tests.csproj
```

Expected: **14 passed, 0 failed.** (One NU1902 warning about a transitive `AngleSharp` advisory is a bUnit dependency — a warning, not a failure.)

Build gates (both must stay at 0 warnings / 0 errors — the repo builds with `TreatWarningsAsErrors`):

```bash
dotnet build clients/admin-blazor/FSH.Admin.Wasm/FSH.Admin.Wasm.csproj
dotnet build clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj
```

---

## 2.8 Files Reference (What You Just Learned)

| File | What it teaches |
|------|----------------|
| `clients/BlazorShared/Models/Identity/UserDtos.cs` | Records, `PagedResult<T>` envelope |
| `clients/BlazorShared/Models/Identity/RoleDtos.cs` | `RoleDto` (id/name/description/permissions) |
| `clients/BlazorShared/Models/SearchRequest.cs` | The page→service query contract |
| `clients/BlazorShared/Services/IUserService.cs`, `UserService.cs` | Query-string building, HTTP + JSON, cancellation |
| `clients/BlazorShared/Services/IRoleService.cs`, `RoleService.cs` | `ListAsync` (used by the role filter) |
| `clients/admin-blazor/FSH.Admin.Wasm/Pages/Identity/Users/UsersListPage.razor(.cs)` | Server-mode table, debounce, filters, row click, error separation |
| `.../UserCreateDialog.razor(.cs)` | MudForm + FluentValidation + dialog result |
| `.../UserDetailPage.razor(.cs)` | Multi-card layout, pending-changes, sessions |
| `clients/admin-blazor/FSH.Admin.Wasm.Tests/TestSetup.cs` | The 3 bUnit-2 traps (loose JS, auth, async dispose) |
| `clients/admin-blazor/FSH.Admin.Wasm.Tests/Pages/Identity/Users/*.cs` | The 14 tests, incl. the popover/dialog harnesses |
| `clients/admin-blazor/FSH.Admin.Wasm/Program.cs` | DI: services, `IDialogService`, `ISnackbar` come from MudBlazor's `AddMudServices()` |

---

## 2.9 Free Learning Resources

### Beginner Path (no experience)
- **Microsoft Learn — Blazor fundamentals**: components, parameters, lifecycle. The "render tree" concept explains why `StateHasChanged` matters.
- **bUnit docs** (bunit.dev): "Rendering" + "Semantic HTML comparison". Run the docs' own first test to feel the loop.
- **MudBlazor docs**: the `MudTable` and `MudForm` pages have live examples — click "Edit in GitHub" to see the exact markup you're testing.

### Junior Path (knows some C#)
- **MudBlazor test recipes**: GitHub search `MudBlazor bunit test` → the MudBlazor repo itself has `MudBlazor.UnitTests` — read how the library tests its own components (that's the same harness you're writing).
- **NSubstitute docs**: "Setting a return value" + "Received calls". The `Arg.Is` expression-tree limit is documented in the "Argument matchers" gotchas.

### Senior Path (production patterns)
- **Steve Sanderson — Blazor architecture**: component boundaries and why "callback writes parent state" is a smell (our 2.3 bug is a textbook example).
- **Testing library design**: read bUnit's `WaitForHelpers` source (linked from bunit.dev) to understand render pumping — it explains every `WaitForAssertion` timeout you'll ever see.
- **FullStackHero docs repo** (`github.com/fullstackhero/docs`): the architecture docs for this repo — module boundaries, permissions, and API conventions behind the endpoints you're consuming.

---

## 2.10 Chapter Quiz

1. Why does `LoadDataAsync` need `InvokeAsync(StateHasChanged)` after setting `_totalCount`? What would the UI show without it?
2. What is the difference between `SortBy` and `SortLabel` on `MudTableSortLabel`? Which one is correct for server mode?
3. MudTable pages from `0`; the API pages from `1`. Where is the conversion made, and what breaks if it's missing?
4. Why are the filter option strings constants instead of inline Razor strings?
5. In `MudSwitch`, why must a bUnit test use `.Change(true)` instead of `.Click()`?
6. Why do `MudSelect` items not appear in `cut.Markup` when the page is rendered standalone? What's the fix?
7. What does `reference.Result` represent in the dialog tests, and why does the service-failure test assert `IsCompleted == false`?
8. NSubstitute: why can't you write `Arg.Is<string>(s => s is not null)`? What's the alternative?
9. A new field is added to the API's user payload. List every place that must change on the Blazor side. (Answer: DTO → service (if the URL changes) → page markup → tests → this doc.)
10. What is the mock failure trap, and what bug did it expose in the role-load error handling?

---

## 2.11 What's Next

You've built the full Users feature: data layer, server-mode table, dialog form, detail page, and a test suite that already caught two real bugs (the missing re-render and the error-overwrite).

Next, Chapter 3 builds the **Roles feature** — the same patterns, plus the permission editor (a nested `MudTreeView` with checkboxes, inherited permissions, and a save-diff), where the page/service seam gets its first real workout.

---

## 2.12 Troubleshooting — Console & Runtime Issues We Hit

Short symptoms → causes → fixes. These were all hit while running both Blazor WASM apps against the live API (and fixed in the same session).

| # | Console symptom | Cause | Fix |
|---|-----------------|-------|-----|
| 1 | `GET /api/v1/realtime/stream 404` | Dead endpoint. SSE uses a **two-step token flow**: browsers' `EventSource` can't send an `Authorization` header, so the server issues a short-lived opaque token first | `POST /api/v1/sse/token` (JWT) → `{ token }` → `GET /api/v1/sse/stream?token=<guid>` (anonymous, single-use, 30s TTL, 15s heartbeats). See `SseService.ConnectOnceAsync` |
| 2 | `POST /api/v1/sse/token 401` + `SSE connection failed` at startup / after logout | SSE attempted while **signed out** (no token) — App.razor connected unconditionally in `OnInitializedAsync` and retried on every token change | Gate on token presence: connect only when `ITokenStore.GetAccessTokenAsync()` is non-null; `StopAsync()` when tokens are cleared (logout). See `App.razor.cs` `EnsureSseConnectionAsync` |
| 3 | SSE connects once, then silently stops after a blip / server restart | Stream read ended and nothing reconnected | `SseService` now auto-reconnects with capped backoff (1s → 2s → … → 30s, reset on success); a dropped stream ends in a retry, not silence |
| 4 | `Missing <MudPopoverProvider />` + MudSelect items / MudDatePicker never open | MudBlazor 9 renders popovers through `MudPopoverProvider` — it was absent from `App.razor` (both apps) | Add `<MudPopoverProvider />` next to `<MudDialogProvider />` / `<MudSnackbarProvider />`. Same requirement in bUnit (see 2.6.4) |
| 5 | `MUD0002: Illegal Attribute 'Autocomplete'` (build warning → fails `TreatWarningsAsErrors`) | `MudTextField` has **no** `Autocomplete` parameter; Pascal-case attributes are rejected by the analyzer | Use lowercase `autocomplete="email"` / `autocomplete="current-password"` — passthrough HTML attributes, and the console's "input elements should have autocomplete attributes" warning goes away |
| 6 | `info: …DefaultAuthorizationService … Authorization failed: DenyAnonymousAuthorizationRequirement` | **Benign**: the *client-side* `AuthorizeRouteView` policy check runs against the anonymous principal on every route visit while signed out (it runs in the browser — `AddAuthorizationCore` registers `DefaultAuthorizationService` in WASM) | Nothing to fix. Optional: silence the info noise with `builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning)` in `Program.cs` |
| 7 | `[Violation] 'setTimeout' handler took Nms` + `CSS Hot Reload ignoring … >7000 rules` | Dev-mode noise: browser profiling warnings and MudBlazor's huge stylesheet being skipped by hot reload | Ignore. Both vanish when you stop `dotnet run` / disable DevTools performance recording |
| 8 | Login succeeds, but `/users`, `/roles`, `/tenants` bounce straight back to `/login` | The JWT only carries **role** claims — permissions are resolved server-side per role (`GET /api/v1/identity/permissions`). The principal was built from JWT claims alone, so every `[Authorize(Policy=…)]` route failed and `RedirectToLogin` fired | `AuthStateProvider` now hydrates `permission` claims from `IPermissionsProvider` (memory → localStorage → endpoint) and maps `role` → `ClaimTypes.Role`. `Program.cs` warms the cache before the first render. `RedirectToLogin` keeps the `returnUrl` and shows a **403 surface** when already signed in (React parity — no more login loop) |
| 9 | Dashboard tiles "Total Users" / "Active Tenants" show `-` | `OverviewPage.razor` was a static stub with no data calls | Rebuilt with the four React-parity tiles — **Tenants** (search `PageNumber=1&PageSize=1` → `totalCount`), **Plans** (`/billing/plans?includeInactive=true` → count + active), **Invoices** (`/billing/invoices?pageNumber=1&pageSize=50` → items count + `totalCount` ledger), **Outstanding** (items with `status == "Issued"`, warning tone when > 0). Added `InvoiceDto` + `GetInvoicesAsync` to `IBillingService` |
| 10 | Column sort only ever sorts ascending (Users table) | `UserService.SearchAsync` always sent `&Sort={SortBy}` and dropped the direction; the server uses a `-` prefix for descending | Prepend `-` when `SortDirection == "desc"`. Also fixed `RoleService` permission endpoints, which were missing the `/roles` segment (`/api/v1/identity/{id}/permissions` → 404; now `/api/v1/identity/roles/{id}/permissions`) |
| 11 | VS tabs show `01-plan.md` for every phase — impossible to tell apart | Files were named `01-plan.md` everywhere | Renamed to `plan.md` per phase (`Phase 0/01 Pre.md` → `pre-plan.md`); the only references lived in `00-Index.md` and were updated |
| 12 | `DeserializeUnableToConvertValue … BillingPlanDto Path: $[0].interval` + dashboard shows the error banner instead of tiles | The API serializes `PlanInterval` as the **enum name string** (`"Monthly"`/`"Yearly"`), but `BillingPlanDto` declared `int Interval`; also `OverviewPage` used `Task.WhenAll`, so one bad fetch blanked all four tiles | DTO: `string Interval` (React parity — `interval: "Monthly" \| "Yearly"`); `PlanLabel` compares `== "Yearly"`. Overview page now awaits each fetch in its own try/catch — a failed source shows only its own error, the other tiles still render |
| 13 | After login the sidebar only shows Dashboard — Users/Roles/Tenants never appear (until F5) | `MainLayout` computed the permission-based nav filter **once** in `OnInitializedAsync` — but the layout instance is created while anonymous (on `/login`) and **survives auth-state changes**; Blazor never re-runs `OnInitializedAsync` for it | Subscribe to `AuthenticationStateProvider.AuthenticationStateChanged` and recompute the filter on every change (`@implements IDisposable`, unsubscribe in `Dispose`). Same trap applies to any permission-derived UI in a layout |
| 14 | `GET /api/v1/identity/permissions 401` + `warn: PermissionsProvider Failed to fetch permissions` right at startup | The eager permission warm-up in `Program.cs` ran even while signed out — no token to send | Gate the warm-up on `ITokenStore.GetAccessTokenAsync()` being non-empty; while signed out there is nothing to hydrate |
| 15 | `Bunit.Rendering.UnknownEventHandlerIdException: There is no event handler with ID 'N' … 'onclick'` — flaky bUnit tests, only under full-suite parallel load | A `FindAll/Find → Click()` raced the renderer replacing the element's event handler (e.g. the submit button re-renders disabled, or the last `Change` leaves a render pending); bUnit's documented workaround is to re-issue the Find after each render | Poll instead of fire: wrap `FindAll → Click` + the mock assertion inside a `WaitForAssertion` loop, re-finding the element every attempt (e.g. `FirstOrDefault(b => … && !b.HasAttribute("disabled"))?.Click()` — the disabled check also guarantees no double-submit). Applied to `TenantCreateDialogTests` + `TenantsListPageTests.Row_click_navigates_to_tenant_detail`; 39/39 consecutive full-suite runs green after the fix |

> **The one-liner version**: if SSE ever 404s, you're on the wrong URL (use the token exchange); if it 401s, you're connecting while signed out (gate on the token); if it stops silently, your client lacks a reconnect loop; if popovers don't render, you're missing `<MudPopoverProvider />`; if policy-gated pages bounce you to `/login` right after login, the permissions aren't in the principal — hydrate them from `/api/v1/identity/permissions` before the first render; if the sidebar stays half-empty after login, the layout computed its nav once — recompute on `AuthenticationStateChanged`; and if a DTO deserialization blows up, check whether the API sent an enum name string where you declared an `int`. If a bUnit test sporadically fails with `UnknownEventHandlerIdException`, your Find→Click raced a re-render — re-find the element inside a `WaitForAssertion` poll loop instead.

---

## Corrections (Phase 7 parity sprint + hardening)

- **Detail routes: plain `{Id}`, never `{Id:guid}`.** Pages bind `[Parameter] string Id`; a `:guid` constraint makes the Router pass a `Guid` object → `Arg_InvalidCastException` ("Unable to set property 'Id'") and the Blazor error UI. This bit Users + Roles detail pages; fixed by dropping the constraint. Every new detail route needs a `Router`-level regression test (`RouteBindingRegressionTests` pattern), because `Render<T>(p => p.Add(x => x.Id, ...))` bypasses route binding entirely.
- **Nav policy flipped to full React parity.** The old "remove dead links for unimplemented pages" rule is gone: every React menu entry exists in Blazor, permission-gated, and lands on the 404 page until its phase lands. The gap tables live in `Phase-07-Parity-Completion/plan.md`.
- **Notifications.** `FshNotificationBell` (shared component, topbar, both apps) replaces any nav-badge plan: unread count (cap 99+), list of 20, mark-read on click + navigate, mark-all-read, refresh on open, SignalR `NotificationCreated` subscription. Backed by `INotificationService` (endpoints `GET /api/v1/notifications/unread-count`, `GET /api/v1/notifications`, `POST /{id}/read`, `POST /read-all`). The inbox **page** is still pending (2.8).
- **Permission constant quirk:** `WebhooksPermissions.Subscriptions.View` has the value `"Permissions.Webhooks.View"` — always check the server constant; class name and value can diverge.
- **bUnit 2.0** — `Render<T>` (not `RenderComponent<T>`); `BunitContext` (not `TestContext`); boolean attributes like `aria-expanded` are normalized (assert presence, not value).
