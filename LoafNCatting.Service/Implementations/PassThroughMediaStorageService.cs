using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;

namespace LoafNCatting.Service.Implementations;

public sealed class PassThroughMediaStorageService : IMediaStorageService
{
    public static PassThroughMediaStorageService Instance { get; } = new();

    private PassThroughMediaStorageService()
    {
    }

    public PresignedUploadDto CreateUploadUrl(MediaAssetKind kind, PresignedUploadRequestDto request)
    {
        throw new InvalidOperationException("Media upload storage is not configured.");
    }

    public string? NormalizeStoredKey(string? value) => value?.Trim();

    public string? ResolveDisplayUrl(string? value) => value?.Trim();
}
