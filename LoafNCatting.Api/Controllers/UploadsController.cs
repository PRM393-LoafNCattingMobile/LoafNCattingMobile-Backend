using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/uploads")]
public class UploadsController(
    IMediaStorageService mediaStorage,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpPost("avatar")]
    public ActionResult<PresignedUploadDto> CreateAvatarUploadUrl(PresignedUploadRequestDto request)
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return CreateUpload(MediaAssetKind.Avatar, request);
    }

    [HttpPost("product")]
    public ActionResult<PresignedUploadDto> CreateProductUploadUrl(PresignedUploadRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return CreateUpload(MediaAssetKind.Product, request);
    }

    [HttpPost("cat")]
    public ActionResult<PresignedUploadDto> CreateCatUploadUrl(PresignedUploadRequestDto request)
    {
        if (!SessionAuthorization.TryRequireStaffOrAdmin(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        return CreateUpload(MediaAssetKind.Cat, request);
    }

    private ActionResult<PresignedUploadDto> CreateUpload(MediaAssetKind kind, PresignedUploadRequestDto request)
    {
        try
        {
            return Ok(mediaStorage.CreateUploadUrl(kind, request));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
