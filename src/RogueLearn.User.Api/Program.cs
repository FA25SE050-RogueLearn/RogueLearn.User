using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using DotNetEnv;
using BuildingBlocks.Shared.Authentication;
using Microsoft.SemanticKernel;
using System.Text.Json.Serialization; // ADD THIS USING

// Load environment variables from .env file
Env.Load();

// Configure Serilog
Log.Logger = SerilogConfiguration.CreateLogger();

try
{
    Log.Information("Starting RogueLearn.User API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the host
    builder.Host.UseSerilog();

    // Add our shared, centralized authentication and authorization services.
    builder.Services.AddRogueLearnAuthentication(builder.Configuration);

    // --- RE-INTRODUCED: SEMANTIC KERNEL (AI) SERVICE CONFIGURATION ---
    builder.Services.AddScoped<Kernel>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        // Create a Custom HTTP Client to increase timeout for AI calls
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };

        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.Services.AddSingleton(loggerFactory);

        var provider = configuration["AI:Provider"];
        switch (provider)
        {
            case "Google":
                var googleModel = configuration["AI:Google:Model"] ?? throw new InvalidOperationException("AI:Google:Model is not configured.");
                var googleApiKey = configuration["AI:Google:ApiKey"] ?? throw new InvalidOperationException("AI:Google:ApiKey is not configured.");
                kernelBuilder.AddGoogleAIGeminiChatCompletion(modelId: googleModel, apiKey: googleApiKey, httpClient: httpClient);
                break;
            default:
                throw new InvalidOperationException($"AI Provider '{provider}' is not supported.");
        }

        return kernelBuilder.Build();
    });

    // Add other services to the container
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices();

    // MODIFIED: Chain the AddApiServices call to configure JSON options
    builder.Services.AddApiServices()
        .ConfigureHttpJsonOptions(options =>
        {
            // This converter allows the API to accept and return enums as strings (e.g., "Bachelor")
            // instead of integers (e.g., 1).
            options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(options =>
        {
            // This does the same for controllers that use MVC's formatter (e.g., returning Ok(object)).
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

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

    app.UseAuthentication();
    app.UseAuthorization();

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

// Make the Program class accessible for integration testing
public partial class Program { }