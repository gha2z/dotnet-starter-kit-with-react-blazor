using FSH.BlazorShared.Models.Files;

namespace FSH.BlazorShared.Services;

public interface IFileService
{
    Task<PresignedUploadResponse> RequestUploadUrlAsync(RequestUploadUrlRequest request, CancellationToken ct = default);
    Task<FileAssetDto> FinalizeUploadAsync(Guid fileAssetId, CancellationToken ct = default);
    Task<FileAssetDto> GetFileMetadataAsync(Guid fileAssetId, CancellationToken ct = default);
    Task<PresignedDownloadResponse> GetFileDownloadUrlAsync(Guid fileAssetId, bool inline = false, CancellationToken ct = default);
    Task<IReadOnlyList<FileAssetDto>> ListMyFilesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<FileAssetDto>> ListSharedFilesAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<IReadOnlyList<FileAssetDto>> ListTrashedFilesAsync(int pageNumber = 1, int pageSize = 20, CancellationToken ct = default);
    Task<FileAssetDto> ChangeVisibilityAsync(Guid fileAssetId, FileVisibility visibility, CancellationToken ct = default);
    Task DeleteAsync(Guid fileAssetId, CancellationToken ct = default);
    Task RestoreAsync(Guid fileAssetId, CancellationToken ct = default);
}
