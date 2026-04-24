using System.Security.Claims;

namespace FamilyTreeApiV2.Common;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("NameIdentifier claim is missing from JWT.");
        return Guid.Parse(value);
    }
}
