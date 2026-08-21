using System.Text.Json.Serialization;

namespace FSH.BlazorShared.Models.Files;

// ─── Enums ────────────────────────────────────────────────────────────────

[JsonConverter(typeof(JsonStringEnumConverter<FileAssetStatus>))]
public enum FileAssetStatus { PendingUpload, Available, Quarantined }

[JsonConverter(typeof(JsonStringEnumConverter<FileVisibility>))]
public enum FileVisibility { Public, Private }

// ─── Response DTOs ────────────────────────────────────────────────────────

public sealed record FileAssetDto(
    Guid Id,
    string OwnerType,
    Guid? OwnerId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    FileVisibility Visibility,
    FileAssetStatus Status,
    int ScanStatus,
    DateTime CreatedAtUtc,
    string? PublicUrl,
    string CreatedByUserId = "",
    DateTimeOffset? DeletedOnUtc = null,
    string? DeletedBy = null);

public sealed record PresignedUploadResponse(
    Guid FileAssetId,
    Uri UploadUrl,
    IReadOnlyDictionary<string, string> RequiredHeaders,
    DateTimeOffset ExpiresAt);

public sealed record PresignedDownloadResponse(
    Uri Url,
    DateTimeOffset ExpiresAt);

// ─── Request DTOs ─────────────────────────────────────────────────────────

public sealed record RequestUploadUrlRequest(
    string OwnerType,
    Guid? OwnerId,
    string FileName,
    string ContentType,
    long SizeBytes,
    FileVisibility Visibility = FileVisibility.Private,
    string Category = "Document");

public sealed record ChangeVisibilityRequest(FileVisibility Visibility);
