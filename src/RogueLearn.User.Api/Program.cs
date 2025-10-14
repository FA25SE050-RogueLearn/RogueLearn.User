using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using System.Text;
using System.Security.Claims;
using DotNetEnv;
using Microsoft.SemanticKernel;

// Load environment variables from .env file
Env.Load();

// Configure Serilog
Log.Logger = SerilogConfiguration.CreateLogger();

try
{
  Log.Information("Starting RogueLearn.User API");

  var builder = WebApplication.CreateBuilder(args);

  // Add Serilog
  builder.Host.UseSerilog();

  // Add AI services
  builder.Services.AddScoped<Kernel>(sp =>
  {
    // Get the required services
    var configuration = sp.GetRequiredService<IConfiguration>();
    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

    // Create a Custom HTTP Client to increase timeout
    var httpClient = new HttpClient
    {
      Timeout = TimeSpan.FromMinutes(2)
    };

    // Create a new Kernel builder
    var kernelBuilder = Kernel.CreateBuilder();

    // Add logging to the Kernel
    kernelBuilder.Services.AddSingleton(loggerFactory);

    // Get the provider from configuration
    var provider = configuration["AI:Provider"];

    switch (provider)
    {
      case "Google":
        var googleModel = configuration["AI:Google:Model"] ?? throw new InvalidOperationException("Google Model not configured.");
        var googleApiKey = configuration["AI:Google:ApiKey"] ?? throw new InvalidOperationException("Google API Key not configured.");
        kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: googleModel, apiKey: googleApiKey, httpClient: httpClient);
        break;

      default:
        throw new InvalidOperationException($"AI Provider '{provider}' is not supported.");
    }

    return kernelBuilder.Build();
  });

  // --- ADD JWT AUTHENTICATION SERVICES ---
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
        var supabaseUrl = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured.");
        var supabaseJwtSecret = builder.Configuration["Supabase:JwtSecret"] ?? throw new InvalidOperationException("Supabase JWT Secret not configured.");

        options.Authority = supabaseUrl;
        options.Audience = "authenticated"; // Default Supabase audience
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuer = true,
          ValidIssuer = supabaseUrl + "/auth/v1",
          ValidateAudience = true,
          ValidAudience = "authenticated",
          ValidateLifetime = true,
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret)),
          // Enhanced role claim mapping for better compatibility
          RoleClaimType = ClaimTypes.Role,
          NameClaimType = ClaimTypes.NameIdentifier
        };

        // Enhanced JWT events for better role handling
        options.Events = new JwtBearerEvents
        {
          OnMessageReceived = context =>
            {
              var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

              // Check Authorization header presence and scheme
              if (!context.Request.Headers.TryGetValue("Authorization", out var authHeader) || string.IsNullOrWhiteSpace(authHeader))
              {
                logger.LogWarning("Missing Authorization header for {Method} {Path}", context.Request.Method, context.Request.Path);
              }
              else
              {
                var value = authHeader.ToString();
                var isBearer = value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
                logger.LogDebug("Authorization header present for {Method} {Path}; Bearer scheme: {IsBearer}", context.Request.Method, context.Request.Path, isBearer);
              }

              return Task.CompletedTask;
            },
          OnTokenValidated = context =>
            {
              var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();

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
              var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
              logger.LogWarning("JWT authentication failed: {Error}", context.Exception.Message);
              return Task.CompletedTask;
            }
        };
      });

  builder.Services.AddAuthorization();
  // --- END AUTHENTICATION SERVICES ---

  builder.Services.AddHttpContextAccessor();

  // Add services to the container
  builder.Services.AddApplication();
  builder.Services.AddInfrastructureServices();
  builder.Services.AddApiServices();

  var app = builder.Build();

  // Configure the HTTP request pipeline
  app.UseMiddleware<ExceptionHandlingMiddleware>();

  if (app.Environment.IsDevelopment())
  {
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
      c.SwaggerEndpoint("/swagger/v1/swagger.json", "RogueLearn.User API V1");
      c.RoutePrefix = string.Empty;
    });

    // Redirect root requests to Swagger UI
    app.Use(async (context, next) =>
    {
      if (context.Request.Path == "/")
      {
        context.Response.Redirect("/index.html");
        return;
      }
      await next();
    });
  }

  app.UseCors("AllowAll");
  app.UseHttpsRedirection();

  // --- ADD AUTHENTICATION AND AUTHORIZATION MIDDLEWARE ---
  // IMPORTANT: These must be called after UseCors and before MapControllers
  app.UseAuthentication();
  app.UseAuthorization();
  // --- END MIDDLEWARE ---

  app.MapControllers();

  Log.Information("RogueLearn.User API started successfully");
  app.Run();
}
catch (Exception ex)
{
  Log.Fatal(ex, "RogueLearn.User API terminated unexpectedly");
}
finally
{
  Log.CloseAndFlush();
}

// Make the Program class accessible for testing
public partial class Program { }
