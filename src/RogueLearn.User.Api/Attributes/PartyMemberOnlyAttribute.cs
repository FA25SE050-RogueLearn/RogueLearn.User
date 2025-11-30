using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using BuildingBlocks.Shared.Authentication;
using System.Security.Claims;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class PartyMemberOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _routeParameterName;

    public PartyMemberOnlyAttribute(string routeParameterName = "partyId")
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

        Guid authUserId;
        try
        {
            authUserId = context.HttpContext.User.GetAuthUserId();
        }
        catch
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

        // Active member authorized (Leader or Member)
    }
}