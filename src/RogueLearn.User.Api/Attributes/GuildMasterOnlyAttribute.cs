using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Interfaces;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GuildMasterOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _routeParameterName;

    public GuildMasterOnlyAttribute(string routeParameterName = "guildId")
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

        // Extract guildId from route
        if (!context.RouteData.Values.TryGetValue(_routeParameterName, out var rawGuildId) || rawGuildId is null)
        {
            context.Result = new BadRequestObjectResult($"Missing route parameter '{_routeParameterName}'.");
            return;
        }

        if (!Guid.TryParse(rawGuildId.ToString(), out var guildId))
        {
            context.Result = new BadRequestObjectResult($"Invalid '{_routeParameterName}' format.");
            return;
        }

        // Extract auth user id using shared extension (consistent with AdminOnlyAttribute)
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

        // Check guild master via repository only (no platform admin override)
        var memberRepo = context.HttpContext.RequestServices.GetService(typeof(IGuildMemberRepository)) as IGuildMemberRepository;
        if (memberRepo == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }   

        var isMaster = await memberRepo.IsGuildMasterAsync(guildId, authUserId, context.HttpContext.RequestAborted);
        if (!isMaster)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Authorized as GuildMaster
    }
}