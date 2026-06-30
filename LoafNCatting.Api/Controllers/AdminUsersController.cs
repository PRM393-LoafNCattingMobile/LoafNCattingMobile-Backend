using LoafNCatting.Api.Infrastructure;
using LoafNCatting.Service.Auth;
using LoafNCatting.Service.DTOs;
using LoafNCatting.Service.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
public class AdminUsersController(
    IAdminUserService service,
    ISessionTokenService sessions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<AdminUserDto>>> GetUsers(
        [FromQuery(Name = "role")] int? role,
        [FromQuery] string? search,
        [FromQuery] bool? active)
    {
        if (!SessionAuthorization.TryRequireAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        return Ok(await service.GetUsersAsync(role, search, active));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<AdminUserDto>> CreateStaff(
        AdminCreateStaffDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var user = await service.CreateStaffAsync(request);
        return user is null
            ? BadRequest(new { message = "Staff data is invalid or email/phone already exists." })
            : Ok(user);
    }

    [HttpPut("{id:int}/role")]
    public async Task<ActionResult<AdminUserDto>> UpdateRole(
        int id,
        AdminUserRoleDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var user = await service.UpdateRoleAsync(id, request);
        return user is null
            ? BadRequest(new { message = "User was not found or role change is not allowed." })
            : Ok(user);
    }

    [HttpPut("{id:int}/active")]
    public async Task<ActionResult<AdminUserDto>> UpdateActive(
        int id,
        AdminUserActiveDto request)
    {
        if (!SessionAuthorization.TryRequireAdmin(
                Request,
                sessions,
                out _,
                out var failure))
        {
            return failure!;
        }

        var user = await service.UpdateActiveAsync(id, request);
        return user is null
            ? BadRequest(new { message = "User was not found or active-state change is not allowed." })
            : Ok(user);
    }
}
