using System.Net.Http.Json;
using FSH.BlazorShared.Models.Files;

namespace FSH.BlazorShared.Services;

public sealed class FileService(HttpClient http) : IFileService
{
    private const string Base = "/api/v1/files";

    private static string QueryString(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}");
        return string.Join("&", parts);
    }

    public async Task<PresignedUploadResponse> RequestUploadUrlAsync(RequestUploadUrlRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync($"{Base}/upload-url", request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PresignedUploadResponse>(ct)
               ?? throw new InvalidOperationException("Failed to get upload URL.");
    }

    public async Task<FileAssetDto> FinalizeUploadAsync(Guid fileAssetId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/{fileAssetId}/finalize", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FileAssetDto>(ct)
               ?? throw new InvalidOperationException("Failed to finalize upload.");
    }

    public async Task<FileAssetDto> GetFileMetadataAsync(Guid fileAssetId, CancellationToken ct = default) =>
        await http.GetFromJsonAsync<FileAssetDto>($"{Base}/{fileAssetId}", ct)
        ?? throw new InvalidOperationException("File not found.");

    public async Task<PresignedDownloadResponse> GetFileDownloadUrlAsync(Guid fileAssetId, bool inline = false, CancellationToken ct = default)
    {
        var query = inline ? "?inline=true" : string.Empty;
        return await http.GetFromJsonAsync<PresignedDownloadResponse>($"{Base}/{fileAssetId}/url{query}", ct)
               ?? throw new InvalidOperationException("Failed to get download URL.");
    }

    public async Task<IReadOnlyList<FileAssetDto>> ListMyFilesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = QueryString(("page", page.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<FileAssetDto>>($"{Base}/mine?{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<FileAssetDto>> ListSharedFilesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = QueryString(("page", page.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<FileAssetDto>>($"{Base}/shared?{query}", ct) ?? [];
    }

    public async Task<IReadOnlyList<FileAssetDto>> ListTrashedFilesAsync(int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = QueryString(("pageNumber", pageNumber.ToString()), ("pageSize", pageSize.ToString()));
        return await http.GetFromJsonAsync<IReadOnlyList<FileAssetDto>>($"{Base}/trash?{query}", ct) ?? [];
    }

    public async Task<FileAssetDto> ChangeVisibilityAsync(Guid fileAssetId, FileVisibility visibility, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync($"{Base}/{fileAssetId}/visibility", new ChangeVisibilityRequest(visibility), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<FileAssetDto>(ct)
               ?? throw new InvalidOperationException("Failed to change visibility.");
    }

    public async Task DeleteAsync(Guid fileAssetId, CancellationToken ct = default)
    {
        var response = await http.DeleteAsync($"{Base}/{fileAssetId}", ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task RestoreAsync(Guid fileAssetId, CancellationToken ct = default)
    {
        var response = await http.PostAsync($"{Base}/{fileAssetId}/restore", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
