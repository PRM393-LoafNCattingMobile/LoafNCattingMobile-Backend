using LoafNCatting.Service.DTOs;

namespace LoafNCatting.Service.Interfaces;

public enum MediaAssetKind
{
    Avatar,
    Product,
    Cat
}

public interface IMediaStorageService
{
    PresignedUploadDto CreateUploadUrl(MediaAssetKind kind, PresignedUploadRequestDto request);
    string? NormalizeStoredKey(string? value);
    string? ResolveDisplayUrl(string? value);
}
