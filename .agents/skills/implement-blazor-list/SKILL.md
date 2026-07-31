---
name: implement-blazor-list
description: Create a paged, filterable, sortable MudTable list page. Use when adding any list view. See .agents/rules/frontend/blazor-shared.md.
argument-hint: "[admin|dashboard] [Resource]"
---

# Implement Blazor List

## Step 1 — Service method

```csharp
public async Task<PagedResult<{Resource}Dto>> SearchAsync(SearchRequest request, CancellationToken ct = default)
{
    var query = new Dictionary<string, string?>
    {
        ["PageNumber"] = request.PageNumber.ToString(),
        ["PageSize"] = request.PageSize.ToString(),
        ["Search"] = request.Search,
    };
    if (request.Filters is not null)
        foreach (var (key, value) in request.Filters)
            query[key] = value;

    var url = $"{Base}/search?{string.Join("&", query.Where(kv => kv.Value is not null).Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"))}";
    return await _http.GetFromJsonAsync<PagedResult<{Resource}Dto>>(url, ct)
           ?? new PagedResult<{Resource}Dto>([], 0, 0, 0, 0, false, false);
}
```

## Step 2 — MudTable with pagination

```razor
<MudTable Items="@_items"
          Hover="true"
          FixedHeader="true"
          @bind-PageNumber="_pageNumber"
          PageSizeOptions="@(new[] { 10, 25, 50 })"
          ServerData="(TableState state) => LoadServerDataAsync(state)">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<{Resource}Dto, object>(x => x.Name)">Name</MudTableSortLabel></MudTh>
        <MudTh>Status</MudTh>
        <MudTh Style="width: 48px">Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Name">
            <MudLink @onclick="() => NavigateTo(context.Id)">@context.Name</MudLink>
        </MudTd>
        <MudTd DataLabel="Status">
            <MudChip Color="@GetStatusColor(context.Status)" Size="MudChipSize.Small">@context.Status</MudChip>
        </MudTd>
        <MudTd>
            <MudIconButton Icon="@Icons.Material.Filled.Edit" Size="Size.Small" @onclick="() => Edit(context.Id)" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete" Size="Size.Small" @onclick="() => ConfirmDelete(context)" />
        </MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="@(new[] { 10, 25, 50 })" />
    </PagerContent>
</MudTable>
```

## Step 3 — Server-side data loading

```csharp
private async Task<TableData<{Resource}Dto>> LoadServerDataAsync(TableState state)
{
    _isLoading = true;
    try
    {
        var result = await _service.SearchAsync(new SearchRequest(
            PageNumber: state.Page + 1,
            PageSize: state.PageSize,
            Search: _search,
            SortBy: state.SortLabel,
            SortDirection: state.SortDirection == SortDirection.Descending ? "desc" : "asc"));
        _totalCount = result.TotalCount;
        return new TableData<{Resource}Dto> { Items = result.Items, TotalItems = result.TotalCount };
    }
    catch (ApiRequestException ex)
    {
        _error = ex.Message;
        return new TableData<{Resource}Dto> { Items = [], TotalItems = 0 };
    }
    finally { _isLoading = false; }
}
```

## Step 4 — Filter bar

```razor
<MudToolBar>
    <MudTextField @bind-Value="_search"
                  Placeholder="Search…"
                  Immediate="true"
                  DebounceInterval="300"
                  @bind-Value:after="() => { _pageNumber = 1; _ = LoadData(); }"
                  Variant="Variant.Outlined"
                  Adornment="Adornment.Start"
                  AdornmentIcon="@Icons.Material.Filled.Search" />
    <MudSelect @bind-Value="_statusFilter"
               Label="Status"
               @bind-Value:after="() => { _pageNumber = 1; _ = LoadData(); }">
        <MudSelectItem Value="null">All</MudSelectItem>
        <MudSelectItem Value="Active">Active</MudSelectItem>
        <MudSelectItem Value="Inactive">Inactive</MudSelectItem>
    </MudSelect>
</MudToolBar>
```

## Validation

- [ ] Table loads data on initial render
- [ ] Search/filter triggers re-query with page reset to 1
- [ ] Pagination controls visible when TotalItems > page size
- [ ] Sort by column works
- [ ] Loading state shows skeleton
- [ ] Empty state shows "No results found" message
- [ ] Error state shows MudAlert
