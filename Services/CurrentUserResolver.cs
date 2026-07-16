using System.Security.Claims;

namespace ProductApi.Services;

/// <summary>
/// Extracts the current authenticated user's identity from the
/// HTTP context's JWT claims. Used to populate audit fields.
/// </summary>
public interface ICurrentUserResolver
{
    CurrentUser? Resolve(ClaimsPrincipal principal);
}

public class CurrentUserResolver : ICurrentUserResolver
{
    public CurrentUser? Resolve(ClaimsPrincipal principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
            return null;

        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? principal.FindFirst("user_id")?.Value
                 ?? principal.FindFirst("sub")?.Value;

        var name = principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? principal.FindFirst("unique_name")?.Value
                   ?? "Unknown";

        if (string.IsNullOrEmpty(id))
            return null;

        return new CurrentUser(id, name);
    }
}
