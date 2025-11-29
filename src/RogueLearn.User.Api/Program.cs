// RogueLearn.User/src/RogueLearn.User.Api/Program.cs
using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using BuildingBlocks.Shared.Authentication;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using RogueLearn.User.Api.HealthChecks;
using RogueLearn.User.Api.Utilities;
using RogueLearn.User.Application.Services;

// Load environment variables from .env file

EnvironmentHelper.LoadConfiguration();

// Configure Serilog
Log.Logger = SerilogConfiguration.CreateLogger();

try
{
    Log.Information("Starting RogueLearn.User API");

    var builder = WebApplication.CreateBuilder(args);

    // --- DEBUGGING: Check if environment variables are loaded ---
    Console.WriteLine($"[DEBUG] AI Provider from ENV: {Environment.GetEnvironmentVariable("AI__Provider")}");
    Console.WriteLine($"[DEBUG] Google Model from ENV: {Environment.GetEnvironmentVariable("AI__Google__Model")}");
    Console.WriteLine($"[DEBUG] Google ApiKey from ENV: {(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AI__Google__ApiKey")) ? "YES" : "NO")}");


    // Check if IConfiguration can read them
    var config = builder.Configuration;
    Console.WriteLine($"[DEBUG] AI Provider from IConfiguration: {config["AI:Provider"]}");
    Console.WriteLine($"[DEBUG] Google Model from IConfiguration: {config["AI:Google:Model"]}");
    var googleApiKey = config["AI:Google:ApiKey"];
    Console.WriteLine($"[DEBUG] Google ApiKey from IConfiguration: {(!string.IsNullOrEmpty(googleApiKey) ? "YES (Length: " + googleApiKey?.Length + ")" : "NO")}");
    // --- END DEBUGGING ---

    // Add Serilog to the host
    builder.Host.UseSerilog();
    // --- THIS CONFIGURATION IS STILL REQUIRED ---
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Port 6968: HTTP/1.1 and HTTP/2 for REST API, Swagger, and gRPC
        options.ListenAnyIP(6968, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http1AndHttp2;
            listenOptions.UseHttps();
        });

        // Port 6969: HTTP/2 only for pure gRPC clients
        options.ListenAnyIP(6969, listenOptions =>
        {
            listenOptions.Protocols = HttpProtocols.Http2;
        });
    }); 
    

    // Add our shared, centralized authentication and authorization services.
    builder.Services.AddRogueLearnAuthentication(builder.Configuration);
    
    var supabaseConnStr =  builder.Configuration["Supabase:ConnStr"];
    //  ADD THIS INSTEAD:
    builder.Services.AddHangfire(configuration => configuration
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseInMemoryStorage());

    builder.Services.AddScoped<IQuestStepGenerationService, QuestStepGenerationService>();
    builder.Services.AddHangfireServer();

    builder.Services.AddHangfireServer();
    // --- SEMANTIC KERNEL (AI) SERVICE CONFIGURATION ---
    builder.Services.AddScoped(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("SemanticKernel");

        // Create a Custom HTTP Client to increase timeout for AI calls
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(loggerFactory);

        var provider = configuration["AI:Provider"];
        logger.LogInformation("Configuring AI Provider: {Provider}", provider);

        switch (provider)
        {
            case "Google":
                var googleModel = configuration["AI:Google:Model"] ?? throw new InvalidOperationException("AI:Google:Model is not configured.");
                var key = configuration["AI:Google:ApiKey"] ?? throw new InvalidOperationException("AI:Google:ApiKey is not configured.");

                logger.LogInformation("Google Model: {Model}, API Key Length: {Length}", googleModel, key.Length);

                kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: googleModel, apiKey: key, httpClient: httpClient);
                break;
            default:
                throw new InvalidOperationException($"AI Provider '{provider}' is not supported.");
        }

        return kernelBuilder.Build();
    });

    // Add other services to the container
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplication(builder.Configuration);
    builder.Services.AddInfrastructureServices();

    // Add response caching services
    builder.Services.AddResponseCaching();

    builder.Services.AddControllers(options =>
    {
        // Define cache profiles for different scenarios
        options.CacheProfiles.Add("Default30",
            new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = 30,
                Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
                VaryByHeader = "Authorization"
            });

        options.CacheProfiles.Add("Default60",
            new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = 60,
                Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
                VaryByHeader = "Authorization"
            });

        options.CacheProfiles.Add("Default300",
            new Microsoft.AspNetCore.Mvc.CacheProfile
            {
                Duration = 300, // 5 minutes
                Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any,
                VaryByHeader = "Authorization"
            });
    })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
    });

    builder.Services.AddApiServices();

    builder.Services.AddGrpc();
    
// Health check registration
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
        .AddCheck<SupabaseHealthCheck>(
            "supabase",
            failureStatus: HealthStatus.Unhealthy,
            tags: new[] { "db" });
    
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddGrpcReflection();
    }

    var app = builder.Build();

    // Configure the HTTP request pipeline
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Enable Swagger in all environments (Development and Production)
    app.UseSwagger(c =>
    {
        c.RouteTemplate = "swagger/{documentName}/swagger.json";

        // MODIFICATION: The PreSerializeFilters block has been removed entirely
        // to prevent the '/user-service' prefix from being added to the server URLs
        // in the generated swagger.json file.
    });
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RogueLearn.User API V1");
        c.RoutePrefix = string.Empty;
    });
    
    app.UseCors("AllowAll");
    app.UseHttpsRedirection();

    // Enable response caching middleware (must be before Authentication)
    app.UseResponseCaching();

    app.UseAuthentication();
    app.UseAuthorization();
    
    // Simple text "OK" health check
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("live"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync(report.Status == HealthStatus.Healthy ? "OK" : "UNHEALTHY");
        }
    });

    // Supabase-specific health check with detailed response
    app.MapHealthChecks("/health/supabase", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("db"),
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";
        
            var entry = report.Entries.First();
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = $"{entry.Value.Duration.TotalMilliseconds}ms",
                timestamp = DateTime.UtcNow
            });
        
            await context.Response.WriteAsync(result);
        }
    });
    app.MapControllers();

    app.MapGrpcService<RogueLearn.User.Api.GrpcServices.UserProfilesGrpcService>();
    app.MapGrpcService<RogueLearn.User.Api.GrpcServices.UserContextGrpcService>();
    app.MapGrpcService<RogueLearn.User.Api.GrpcServices.AchievementsGrpcService>();
    app.MapGrpcService<RogueLearn.User.Api.GrpcServices.GuildsGrpcService>();
    if (app.Environment.IsDevelopment())
    {
        app.MapGrpcReflectionService();
    }

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

public partial class Program { }