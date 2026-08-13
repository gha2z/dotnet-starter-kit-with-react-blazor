using System.Net.Http.Headers;
using FSH.BlazorShared.Models.Files;
using FSH.BlazorShared.Services;
using Microsoft.Extensions.Logging;

namespace FSH.Hybrid.Services;

public sealed record PickedFile(Stream Stream, string FileName, string ContentType, long SizeBytes);

public interface IMediaPickerService
{
    Task<PickedFile?> PickAsync(string? title = null, CancellationToken ct = default);
    Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default);
}

/// <summary>
/// Native camera/gallery/file picking via MAUI Essentials (no JS interop). The picked
/// stream feeds <see cref="HybridFileUploadService"/>'s presigned upload flow.
/// </summary>
public sealed class MediaPickerService : IMediaPickerService
{
    public async Task<PickedFile?> PickAsync(string? title = null, CancellationToken ct = default)
    {
        var result = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = title ?? "Choose a file",
        });
        return await ToPickedFileAsync(result, ct);
    }

    public async Task<PickedFile?> CapturePhotoAsync(CancellationToken ct = default)
    {
        var result = await MediaPicker.Default.CapturePhotoAsync(new MediaPickerOptions
        {
            Title = "Take a photo",
        });
        return await ToPickedFileAsync(result, ct);
    }

    private static async Task<PickedFile?> ToPickedFileAsync(FileResult? result, CancellationToken ct)
    {
        if (result is null)
        {
            return null;
        }

        var stream = await result.OpenReadAsync();
        var contentType = ResolveContentType(result.FileName, result.ContentType);
        return new PickedFile(stream, result.FileName, contentType, stream.Length);
    }

    private static string ResolveContentType(string fileName, string? contentType)
    {
        if (!string.IsNullOrWhiteSpace(contentType))
        {
            return contentType;
        }

        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".zip" => "application/zip",
            _ => "application/octet-stream",
        };
    }
}

public interface IHybridFileUploadService
{
    Task<FileAssetDto> UploadAsync(PickedFile file, CancellationToken ct = default);
}

/// <summary>
/// Uploads a picked file through the Files module's presigned flow
/// (request URL → PUT to storage → finalize) entirely from native code. The PUT uses the
/// "FSH.Storage" client (no API base address) so the presigned URL is used verbatim.
/// </summary>
public sealed class HybridFileUploadService(
    IFileService fileService,
    IHttpClientFactory httpFactory,
    ILogger<HybridFileUploadService> logger) : IHybridFileUploadService
{
    public async Task<FileAssetDto> UploadAsync(PickedFile file, CancellationToken ct = default)
    {
        var upload = await fileService.RequestUploadUrlAsync(new RequestUploadUrlRequest(
            OwnerType: "MyFiles",
            OwnerId: null,
            FileName: file.FileName,
            ContentType: file.ContentType,
            SizeBytes: file.SizeBytes), ct);

        using var storageClient = httpFactory.CreateClient("FSH.Storage");
        using var content = new StreamContent(file.Stream);
        content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        foreach (var (key, value) in upload.RequiredHeaders)
        {
            if (!string.Equals(key, "Content-Type", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                content.Headers.TryAddWithoutValidation(key, value);
            }
        }

        var response = await storageClient.PutAsync(upload.UploadUrl, content, ct);
        response.EnsureSuccessStatusCode();

        var finalized = await fileService.FinalizeUploadAsync(upload.FileAssetId, ct);
        logger.LogInformation("Uploaded file {FileAssetId} ({FileName}, {Size} bytes)", finalized.Id, file.FileName, file.SizeBytes);
        return finalized;
    }
}
