using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

namespace BuildingBlocks.Shared.Authentication;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRogueLearnAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        // Get Supabase configuration from appsettings.json or environment variables.
        var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase:Url is not configured.");
        var supabaseJwtSecret = configuration["Supabase:JwtSecret"] ?? throw new InvalidOperationException("Supabase:JwtSecret is not configured.");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = supabaseUrl;
                options.Audience = "authenticated";
                // In a real production environment, this should be true. For local dev/containers, false is often needed.
                options.RequireHttpsMetadata = false;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = $"{supabaseUrl}/auth/v1",
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret)),
                    ClockSkew = TimeSpan.Zero,
                    // Enhance role Claims mapping for better compatibility
                    RoleClaimType = ClaimTypes.Role,
                    NameClaimType = ClaimTypes.NameIdentifier,
                };

                // Enhanced JWT events for better role handling
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();

                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            // Extract roles from custom claims and add them as role claims
                            var rolesClaim = context.Principal.FindFirst("roles")?.Value;
                            if (!string.IsNullOrEmpty(rolesClaim))
                            {
                                try
                                {
                                    // Parse the roles array from JSON
                                    var rolesArray = System.Text.Json.JsonSerializer.Deserialize<string[]>(rolesClaim);
                                    if (rolesArray != null)
                                    {
                                        // Add each role as a separate role claim
                                        foreach (var role in rolesArray)
                                        {
                                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                                        }

                                        logger.LogDebug("Added roles to JWT claims: {Roles}", string.Join(", ", rolesArray));
                                    }
                                }
                                catch (System.Text.Json.JsonException ex)
                                {
                                    logger.LogWarning("Failed to parse roles from JWT: {Error}", ex.Message);
                                }
                            }

                            var userId = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                            var finalRoles = context.Principal?.FindAll(ClaimTypes.Role).Select(c => c.Value);

                            logger.LogDebug("JWT token validated for user {UserId} with final roles: {Roles}",
                                userId, string.Join(", ", finalRoles ?? new string[0]));
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        // Log authentication failures for debugging
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<JwtBearerEvents>>();
                        logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();

        return services;
    }
}
