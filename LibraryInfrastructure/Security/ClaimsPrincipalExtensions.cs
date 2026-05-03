using System;
using System.Security.Claims;

namespace LibraryInfrastructure.Security;

public static class ClaimsPrincipalExtensions
{
    public static int? GetCurrentUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(raw, out var id) ? id : null;
    }

    public static bool IsAdmin(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Admin);
    }

    public static bool CanCreateWorks(this ClaimsPrincipal principal)
    {
        return principal.IsInRole(AppRoles.Author) || principal.IsInRole(AppRoles.Admin);
    }

    public static string ResolveRole(string? role)
    {
        return AppRoles.Normalize(role);
    }
}
