using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Claims;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PartyLeaderOrCoLeaderOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _routeParameterName;

    public PartyLeaderOrCoLeaderOnlyAttribute(string routeParameterName = "partyId")
    {
        _routeParameterName = routeParameterName;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (user?.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        if (!context.RouteData.Values.TryGetValue(_routeParameterName, out var rawPartyId) || rawPartyId is null)
        {
            context.Result = new BadRequestObjectResult($"Missing route parameter '{_routeParameterName}'.");
            return;
        }

        if (!Guid.TryParse(rawPartyId.ToString(), out var partyId))
        {
            context.Result = new BadRequestObjectResult($"Invalid '{_routeParameterName}' format.");
            return;
        }

        var authUserIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? user.FindFirst("sub")?.Value
                            ?? user.FindFirst("user_id")?.Value
                            ?? user.FindFirst("auth_user_id")?.Value;

        if (!Guid.TryParse(authUserIdClaim, out var authUserId))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var memberRepo = context.HttpContext.RequestServices.GetService(typeof(IPartyMemberRepository)) as IPartyMemberRepository;
        if (memberRepo == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var member = await memberRepo.GetMemberAsync(partyId, authUserId, context.HttpContext.RequestAborted);
        if (member is null || member.Status != MemberStatus.Active)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (member.Role != PartyRole.Leader && member.Role != PartyRole.CoLeader)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}