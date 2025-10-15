using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Claims;

namespace RogueLearn.User.Api.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class AdminOnlyAttribute : Attribute, IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        // First, try to get roles from JWT claims (preferred method)
        var roleClaims = context.HttpContext.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        if (!roleClaims.Any())
        {
            // Fallback: also check for "role" claim type for compatibility
            roleClaims = context.HttpContext.User.FindAll("roles").Select(c => c.Value).ToList();
        }

        // If roles are found in JWT claims, use them directly
        if (roleClaims.Any())
        {
            if (roleClaims.Contains("Game Master"))
            {
                return; // User has admin role, allow access
            }
            else
            {
                context.Result = new ForbidResult();
                return;
            }
        }

        // Fallback: Database lookup (for backward compatibility)
        var authIdClaim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(authIdClaim) || !Guid.TryParse(authIdClaim, out var authUserId))
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