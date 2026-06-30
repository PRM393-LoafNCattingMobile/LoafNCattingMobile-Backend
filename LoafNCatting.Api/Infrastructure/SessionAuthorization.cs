using LoafNCatting.Service.Auth;
using Microsoft.AspNetCore.Mvc;

namespace LoafNCatting.Api.Infrastructure;

internal static class SessionAuthorization
{
    public static bool TryRequireSession(
        HttpRequest request,
        ISessionTokenService sessions,
        out UserSession? session,
        out ActionResult? failure)
    {
        var token = ReadToken(request);
        if (string.IsNullOrWhiteSpace(token))
        {
            session = null;
            failure = new UnauthorizedObjectResult(new { message = "Missing session token." });
            return false;
        }

        session = sessions.GetSession(token);
        if (session is null)
        {
            failure = new UnauthorizedObjectResult(new { message = "Session is invalid or expired." });
            return false;
        }

        failure = null;
        return true;
    }

    public static bool TryRequireUserSession(
        HttpRequest request,
        ISessionTokenService sessions,
        int expectedUserId,
        out ActionResult? failure)
    {
        if (!TryRequireSession(request, sessions, out var session, out failure))
        {
            return false;
        }

        if (session!.UserId != expectedUserId)
        {
            failure = new ObjectResult(new { message = "Session does not match the requested user." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return false;
        }

        return true;
    }

    public static bool TryRequireAdmin(
        HttpRequest request,
        ISessionTokenService sessions,
        out UserSession? session,
        out ActionResult? failure)
    {
        return TryRequireAnyRole(request, sessions, ["Admin"], out session, out failure);
    }

    public static bool TryRequireStaffOrAdmin(
        HttpRequest request,
        ISessionTokenService sessions,
        out UserSession? session,
        out ActionResult? failure)
    {
        return TryRequireAnyRole(request, sessions, ["Admin", "Staff"], out session, out failure);
    }

    private static bool TryRequireAnyRole(
        HttpRequest request,
        ISessionTokenService sessions,
        IEnumerable<string> allowedRoles,
        out UserSession? session,
        out ActionResult? failure)
    {
        if (!TryRequireSession(request, sessions, out session, out failure))
        {
            return false;
        }

        if (!allowedRoles.Contains(session!.RoleName.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            session = null;
            failure = new ObjectResult(new { message = "Session role is not allowed for this action." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
            return false;
        }

        failure = null;
        return true;
    }

    private static string? ReadToken(HttpRequest request)
    {
        var authorization = request.Headers.Authorization.ToString();
        if (authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorization["Bearer ".Length..].Trim();
        }

        return request.Headers["X-Session-Token"].ToString();
    }
}
