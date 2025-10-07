using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RogueLearn.User.Api.Extensions;
using RogueLearn.User.Api.Middleware;
using RogueLearn.User.Infrastructure.Extensions;
using RogueLearn.User.Infrastructure.Logging;
using Serilog;
using System.Text;
using DotNetEnv;

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
				IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(supabaseJwtSecret))
			};
		});

	builder.Services.AddAuthorization();
	// --- END AUTHENTICATION SERVICES ---


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
