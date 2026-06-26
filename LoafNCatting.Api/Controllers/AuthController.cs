using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using LoafNCatting.Service.Auth;
using Microsoft.AspNetCore.Mvc;
using LoafNCatting.Api.Infrastructure;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService service, ISessionTokenService sessions) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<EmailVerificationChallengeDto>> Register(RegisterRequestDto request)
    {
        var result = await service.RegisterAsync(request);
        return result is null
            ? Conflict(new { message = "Email or phone number already exists." })
            : Accepted(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var result = await service.LoginAsync(request);
        if (result.Auth is not null)
        {
            return Ok(result.Auth);
        }

        if (result.RequiresEmailVerification)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                message = "Email address is not verified.",
                email = result.Email
            });
        }

        return Unauthorized(new { message = "Invalid login credentials." });
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<AuthResponseDto>> VerifyEmail(VerifyEmailRequestDto request)
    {
        var result = await service.VerifyEmailAsync(request);
        return result is null
            ? BadRequest(new { message = "Invalid or expired verification code." })
            : Ok(result);
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<EmailVerificationChallengeDto>> ResendVerification(ResendVerificationRequestDto request)
    {
        var result = await service.ResendVerificationAsync(request);
        return result is null
            ? NotFound(new { message = "Account not found or email is already verified." })
            : Accepted(result);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        if (!SessionAuthorization.TryRequireSession(Request, sessions, out _, out var failure))
        {
            return failure!;
        }

        var authorization = Request.Headers.Authorization.ToString();
        var token = authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorization["Bearer ".Length..].Trim()
            : Request.Headers["X-Session-Token"].ToString();
        await service.LogoutAsync(token);
        return NoContent();
    }
}


