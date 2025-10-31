using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Claims;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class GuildMasterOrOfficerOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _routeParameterName;

    public GuildMasterOrOfficerOnlyAttribute(string routeParameterName = "guildId")
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

        // Note: Platform Admin (Game Master) is intentionally NOT handled here.
        // Use AdminOnlyAttribute where platform-level authorization is required.

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

        var authUserIdClaim = user.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "auth_user_id")?.Value;
        if (!Guid.TryParse(authUserIdClaim, out var authUserId))
        {
            context.Result = new UnauthorizedObjectResult("Invalid or missing user identifier.");
            return;
        }

        // Check guild role via repository
        var memberRepo = context.HttpContext.RequestServices.GetService(typeof(IGuildMemberRepository)) as IGuildMemberRepository;
        if (memberRepo == null)
        {
            context.Result = new StatusCodeResult(StatusCodes.Status500InternalServerError);
            return;
        }

        var member = await memberRepo.GetMemberAsync(guildId, authUserId, context.HttpContext.RequestAborted);
        if (member is null || member.Status != MemberStatus.Active)
        {
            context.Result = new ForbidResult();
            return;
        }

        if (member.Role != GuildRole.GuildMaster && member.Role != GuildRole.Officer)
        {
            context.Result = new ForbidResult();
            return;
        }

        // Authorized as GuildMaster or Officer
    }
}