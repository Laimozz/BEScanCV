using System.Security.Claims;

namespace BEScanCV.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static long? GetCurrentUserId(this ClaimsPrincipal user)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier) ??
                     user.FindFirstValue("sub") ??
                     user.FindFirstValue("user_id") ??
                     user.FindFirstValue("id");

        return long.TryParse(userId, out var parsedUserId) && parsedUserId > 0
            ? parsedUserId
            : null;
    }
}
