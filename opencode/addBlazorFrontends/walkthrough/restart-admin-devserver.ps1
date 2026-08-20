$ErrorActionPreference = "Stop"
$dotnet = "C:\Program Files\dotnet\dotnet.exe"
$devserver = "C:\Users\user\.nuget\packages\microsoft.aspnetcore.components.webassembly.devserver\10.0.10\build\../tools/blazor-devserver.dll"
$app = "C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\clients\admin-blazor\FSH.Admin.Wasm\bin\Debug\net10.0\FSH.Admin.Wasm.dll"
$wd = "C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\clients\admin-blazor\FSH.Admin.Wasm"
$logs = "C:\repos\Project\dotnet-starter-kit-with-react-blazor-main\opencode\addBlazorFrontends\walkthrough\logs"

$listener = Get-NetTCPConnection -LocalPort 5175 -State Listen -ErrorAction SilentlyContinue
if ($listener) {
    Stop-Process -Id $listener[0].OwningProcess -Force
    Start-Sleep -Seconds 2
}

$env:ASPNETCORE_URLS = "http://localhost:5175"
Start-Process -FilePath $dotnet -ArgumentList @($devserver, "--applicationpath", $app) -WorkingDirectory $wd `
    -RedirectStandardOutput (Join-Path $logs "devserver-admin.log") `
    -RedirectStandardError (Join-Path $logs "devserver-admin.err.log") -WindowStyle Hidden

Start-Sleep -Seconds 4
$check = Get-NetTCPConnection -LocalPort 5175 -State Listen -ErrorAction SilentlyContinue
if ($check) { "DevServer restarted, PID $($check[0].OwningProcess)" } else { "NOT LISTENING" }