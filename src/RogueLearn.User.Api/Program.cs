// RogueLearn.User/src/RogueLearn.User.Api/Program.cs
using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using DotNetEnv;
using BuildingBlocks.Shared.Authentication; // This is our shared authentication logic
using Microsoft.SemanticKernel; // This is required for the new AI features

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
    // This block is necessary for the curriculum import functionality.
    builder.Services.AddScoped<Kernel>(sp =>
    {
        var configuration = sp.GetRequiredService<IConfiguration>();
        var loggerFactory = sp.GetRequiredService<ILoggerFactory>();

        // Create a Custom HTTP Client to increase timeout for AI calls
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

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
            // Add cases for other providers like "AzureOpenAI" or "OpenAI" here in the future.
            default:
                throw new InvalidOperationException($"AI Provider '{provider}' is not supported.");
        }

        return kernelBuilder.Build();
    });
    // --- END OF AI SERVICE CONFIGURATION ---

    // Add other services to the container
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplication();
    await builder.Services.AddInfrastructureServices(builder.Configuration);
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

        // Redirect root requests to Swagger UI for convenience in development
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

    // Add the authentication and authorization middleware to the request pipeline.
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