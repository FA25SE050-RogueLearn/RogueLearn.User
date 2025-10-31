using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Claims;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PartyLeaderOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _partyIdRouteKey;

    /// <summary>
    /// Checks authorization for Party leader-only endpoints. Allows access only if
    /// the caller is the Leader of the party identified by the route value.
    /// Platform admin role is NOT considered here.
    /// </summary>
    /// <param name="partyIdRouteKey">Route key to read partyId from. Defaults to "partyId".</param>
    public PartyLeaderOnlyAttribute(string partyIdRouteKey = "partyId")
    {
        _partyIdRouteKey = partyIdRouteKey;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Platform admin role is not applicable here; only Party Leader is allowed.

        // Extract partyId from route
        if (!context.RouteData.Values.TryGetValue(_partyIdRouteKey, out var partyIdObj) || partyIdObj is null)
        {
            context.Result = new BadRequestObjectResult($"Missing route parameter '{_partyIdRouteKey}'.");
            return;
        }

        if (!Guid.TryParse(partyIdObj.ToString(), out var partyId))
        {
            context.Result = new BadRequestObjectResult($"Invalid '{_partyIdRouteKey}' route parameter.");
            return;
        }

        // Resolve repository and check leader role
        var partyMemberRepo = context.HttpContext.RequestServices.GetRequiredService<IPartyMemberRepository>();
        var authUserIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? user.FindFirst("sub")?.Value
                            ?? user.FindFirst("user_id")?.Value
                            ?? user.FindFirst("auth_user_id")?.Value;

        if (authUserIdClaim is null || !Guid.TryParse(authUserIdClaim, out var authUserId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var isLeader = await partyMemberRepo.IsLeaderAsync(partyId, authUserId, CancellationToken.None);
        if (!isLeader)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Authorized
    }
}