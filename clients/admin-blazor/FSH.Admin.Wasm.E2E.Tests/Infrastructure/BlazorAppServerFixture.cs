using System.Diagnostics;
using Microsoft.Playwright;
using Xunit;

namespace FSH.Admin.Wasm.E2E.Tests.Infrastructure;

/// <summary>
/// Collection fixture for the admin-blazor E2E suite.
/// Owns the Playwright browser instance and the Blazor WASM dev server on port 5175
/// (mirrors the React apps' Playwright webServer: starts the app, reuses one that is
/// already answering). The dev server is started with the launch profile's URL via
/// --urls, with the launch profile disabled so no browser window is popped.
/// </summary>
public sealed class BlazorAppServerFixture : IAsyncLifetime, IDisposable
{
    public const int ServerPort = 5175;
    public const string ServerUrl = "http://localhost:5175";

    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string WasmProjectDir =
        Path.Combine(RepoRoot, "clients", "admin-blazor", "FSH.Admin.Wasm");
    private static readonly string LogDir = Path.Combine(Path.GetTempPath(), "opencode", "e2e");

    private Process? _serverProcess;
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        await StartDevServerAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        try
        {
            if (Browser is not null)
            {
                await Browser.CloseAsync();
            }
        }
        finally
        {
            StopDevServer();
        }
    }

    public void Dispose() => StopDevServer();

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, "clients", "admin-blazor")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the repo root from the test output directory.");
    }

    private static async Task<bool> IsPortRespondingAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.GetAsync($"{ServerUrl}/");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private async Task StartDevServerAsync()
    {
        if (await IsPortRespondingAsync())
        {
            return; // An externally managed dev server is already up — reuse it.
        }

        Directory.CreateDirectory(LogDir);

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = WasmProjectDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        psi.ArgumentList.Add("run");
        psi.ArgumentList.Add("--project");
        psi.ArgumentList.Add(Path.Combine(WasmProjectDir, "FSH.Admin.Wasm.csproj"));
        psi.ArgumentList.Add("--no-launch-profile");
        psi.ArgumentList.Add("--urls");
        psi.ArgumentList.Add(ServerUrl);
        psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Development";

        _serverProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start the admin-blazor dev server.");

        var stdoutPath = Path.Combine(LogDir, "devserver.stdout.log");
        var stderrPath = Path.Combine(LogDir, "devserver.stderr.log");
        _ = Task.Run(() => DrainAsync(_serverProcess.StandardOutput, stdoutPath));
        _ = Task.Run(() => DrainAsync(_serverProcess.StandardError, stderrPath));

        var deadline = DateTime.UtcNow.AddMinutes(6);
        while (DateTime.UtcNow < deadline)
        {
            if (_serverProcess.HasExited)
            {
                throw new InvalidOperationException(
                    $"Dev server exited early (code {_serverProcess.ExitCode}). Logs: {stdoutPath}, {stderrPath}");
            }

            if (await IsPortRespondingAsync())
            {
                return;
            }

            await Task.Delay(2000);
        }

        throw new TimeoutException(
            $"Dev server did not answer on {ServerUrl} within 6 minutes. Logs: {stdoutPath}, {stderrPath}");
    }

    private void StopDevServer()
    {
        if (_serverProcess is { HasExited: false } process)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
                // Process already gone.
            }

            process.Dispose();
            _serverProcess = null;
        }
    }

    private static async Task DrainAsync(StreamReader reader, string logPath)
    {
        await using var writer = new StreamWriter(logPath, append: true) { AutoFlush = true };
        while (await reader.ReadLineAsync() is { } line)
        {
            await writer.WriteLineAsync(line);
        }
    }
}
