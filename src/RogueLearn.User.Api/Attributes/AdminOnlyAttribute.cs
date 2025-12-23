using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Claims;
using BuildingBlocks.Shared.Authentication;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Allow preflight OPTIONS requests to pass through for CORS
        if (context.HttpContext.Request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // Repository lookup is authoritative and handles newly-assigned roles that aren't yet in JWT claims
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

        // Get services from DI container
        var userRoleRepository = context.HttpContext.RequestServices.GetRequiredService<IUserRoleRepository>();
        var roleRepository = context.HttpContext.RequestServices.GetRequiredService<IRoleRepository>();

        try
        {
            // Get user's roles from database
            var userRoles = await userRoleRepository.GetRolesForUserAsync(authUserId, CancellationToken.None);

            // Check if user has any roles
            if (!userRoles.Any())
            {
                context.Result = new ForbidResult();
                return;
            }

            // Get all role details and check if user has admin role
            var hasAdminRole = false;
            foreach (var userRole in userRoles)
            {
                var role = await roleRepository.GetByIdAsync(userRole.RoleId, CancellationToken.None);
                if (role != null && role.Name.Equals("Game Master", StringComparison.OrdinalIgnoreCase))
                {
                    hasAdminRole = true;
                    break;
                }
            }

            if (!hasAdminRole)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
        catch (Exception)
        {
            // If there's an error checking roles, deny access
            context.Result = new ForbidResult();
            return;
        }
    }
}