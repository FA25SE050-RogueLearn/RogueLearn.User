using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using DotNetEnv;
using BuildingBlocks.Shared.Authentication;
using Microsoft.SemanticKernel;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

// Load environment variables from .env file
Env.Load();

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

    // Add our shared, centralized authentication and authorization services.
    builder.Services.AddRogueLearnAuthentication(builder.Configuration);

    // --- SEMANTIC KERNEL (AI) SERVICE CONFIGURATION ---
    builder.Services.AddScoped<Kernel>(sp =>
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
    builder.Services.AddApplication();
    builder.Services.AddInfrastructureServices();

    builder.Services.AddControllers()
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new StringEnumConverter());
            options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        });

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

public partial class Program { }