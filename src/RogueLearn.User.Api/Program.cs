using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using DotNetEnv;
using BuildingBlocks.Shared.Authentication; // Add this using statement

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

	// --- REPLACE THE OLD AUTHENTICATION BLOCK WITH OUR SHARED METHOD ---
	builder.Services.AddRogueLearnAuthentication(builder.Configuration);
	// --- END OF REPLACEMENT ---


	// Add services to the container
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

	// --- CONFIRM AUTHENTICATION AND AUTHORIZATION MIDDLEWARE ARE PRESENT ---
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
