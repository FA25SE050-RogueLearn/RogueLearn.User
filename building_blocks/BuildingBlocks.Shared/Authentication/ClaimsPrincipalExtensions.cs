using System.Security.Claims;

namespace BuildingBlocks.Shared.Authentication;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the authenticated user's ID from the claims principal.
    /// Tries common claim types: NameIdentifier, "sub", "user_id", "auth_user_id".
    /// Throws InvalidOperationException if the claim is missing or not a valid GUID.
    /// </summary>
    /// <param name="principal">The ClaimsPrincipal representing the current user.</param>
    /// <returns>The authenticated user's GUID.</returns>
    /// <exception cref="InvalidOperationException">Thrown when ID claim is missing or invalid.</exception>
    public static Guid GetAuthUserId(this ClaimsPrincipal principal)
    {
        if (principal is null)
            throw new ArgumentNullException(nameof(principal));

        var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)
                      ?? principal.FindFirst("sub")
                      ?? principal.FindFirst("user_id")
                      ?? principal.FindFirst("auth_user_id");

        if (idClaim is null || string.IsNullOrWhiteSpace(idClaim.Value))
            throw new InvalidOperationException("Authenticated user id claim not found in token.");

        if (!Guid.TryParse(idClaim.Value, out var authUserId))
            throw new InvalidOperationException($"Authenticated user id claim is not a valid GUID: {idClaim.Value}");

        return authUserId;
    }
}