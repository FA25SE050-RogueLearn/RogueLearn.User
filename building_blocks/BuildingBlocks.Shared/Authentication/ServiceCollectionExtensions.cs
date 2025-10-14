using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
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
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}