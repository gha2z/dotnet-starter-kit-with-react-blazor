# Setup Checklist — Zero to Running in 10 Minutes

> Paste each command into your terminal. Match the ✅ expected output.

---

## 1. .NET 10 SDK

```powershell
dotnet --version
```
**✅ Expected**: `10.0.xxx` (anything starting with `10.`)

**If missing**: Download from https://dotnet.microsoft.com/download/dotnet/10.0

---

## 2. WASM Tools Workload

```powershell
dotnet workload list
```
**✅ Expected**: `wasm-tools` appears in the list.

**If missing**:
```powershell
dotnet workload install wasm-tools
```

---

## 3. Node.js (for React reference apps)

```powershell
node --version
```
**✅ Expected**: `v22.x.x` or higher.

**If missing**: Download from https://nodejs.org/

---

## 4. IDE (pick one)

| IDE | Verify Command |
|-----|---------------|
| VS Code | `code --version` — ✅ any 1.9x |
| Visual Studio | Open → Help → About — ✅ 2022+ |
| Rider | Open → Help → About — ✅ 2024.x+ |

---

## 5. Clone the Repository

```powershell
git clone <your-repo-url>
cd dotnet-starter-kit-with-react-blazor-main
```

---

## 6. Build Everything

```powershell
# 1. Shared library
dotnet build clients/BlazorShared/FSH.BlazorShared.csproj
```
**✅ Expected**: `Build succeeded. 0 Warning(s) 0 Error(s)`

```powershell
# 2. Admin WASM app
dotnet build clients/admin-blazor/FSH.Admin.Wasm/FSH.Admin.Wasm.csproj
```
**✅ Expected**: `Build succeeded. 0 Warning(s) 0 Error(s)`

```powershell
# 3. Dashboard WASM app
dotnet build clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj
```
**✅ Expected**: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## 7. Run the Admin App

```powershell
dotnet run --project clients/admin-blazor/FSH.Admin.Wasm/FSH.Admin.Wasm.csproj
```
**✅ Expected**: `Now listening on: http://localhost:5175`

Open http://localhost:5175 in your browser. You should see the login page.

---

## 8. Run the Dashboard App (in a second terminal)

```powershell
dotnet run --project clients/dashboard-blazor/FSH.Dashboard.Wasm/FSH.Dashboard.Wasm.csproj
```
**✅ Expected**: `Now listening on: http://localhost:5176`

---

## 9. Run the API (needed for Phase 1+)

```powershell
dotnet run --project src/Host/FSH.Starter.Api
```
**✅ Expected**: API starts on https://localhost:7030 (or 5030)

---

## 10. Run Tests

```powershell
dotnet test clients/admin-blazor/FSH.Admin.Wasm.Tests/
dotnet test clients/dashboard-blazor/FSH.Dashboard.Wasm.Tests/
```

---

## Troubleshooting

| Symptom | Likely Fix |
|---------|-----------|
| `dotnet` not recognized | Install .NET SDK, restart terminal |
| `wasm-tools` not in workload list | `dotnet workload install wasm-tools` |
| Build error: `TargetFramework net10.0` not found | Install .NET 10 SDK |
| App runs but blank page in browser | Check DevTools Console for JS errors; clear browser cache |
| API refuses connection (Phase 1+) | Start API first; check CORS settings in `appsettings.Development.json` |
| MudBlazor CSS missing | Check `index.html` has `_content/MudBlazor/MudBlazor.min.css` link |
