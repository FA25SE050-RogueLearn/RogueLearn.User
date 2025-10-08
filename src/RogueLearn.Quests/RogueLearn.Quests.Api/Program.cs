// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Api/Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using RogueLearn.Quests.Infrastructure.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// --- ADD JWT AUTHENTICATION SERVICES ---
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options =>
	{
		var supabaseUrl = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured.");
		var supabaseJwtSecret = builder.Configuration["Supabase:JwtSecret"] ?? throw new InvalidOperationException("Supabase JWT Secret not configured.");

		options.Authority = supabaseUrl;
		options.Audience = "authenticated";
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

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Application and Infrastructure services
builder.Services.AddMediatR(cfg =>
	cfg.RegisterServicesFromAssembly(typeof(RogueLearn.Quests.Application.Features.QuestLines.Commands.GenerateFromCurriculumCommand).Assembly));

builder.Services.AddQuestsInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();