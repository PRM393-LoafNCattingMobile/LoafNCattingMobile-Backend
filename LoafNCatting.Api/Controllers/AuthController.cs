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
    public async Task<ActionResult<AuthResponseDto>> Register(RegisterRequestDto request)
    {
        var result = await service.RegisterAsync(request);
        return result is null ? Conflict(new { message = "Email or phone number already exists." }) : Ok(result);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequestDto request)
    {
        var result = await service.LoginAsync(request);
        return result is null ? Unauthorized(new { message = "Invalid login credentials." }) : Ok(result);
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


