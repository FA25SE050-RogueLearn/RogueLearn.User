using FluentValidation;
using MediatR;
using Microsoft.OpenApi.Models;
using RogueLearn.User.Application.Behaviours;
using RogueLearn.User.Application.Features.Products.Commands.CreateProduct;
using RogueLearn.User.Application.Mappings;
using System.Reflection;

namespace RogueLearn.User.Api.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddApplication(this IServiceCollection services)
	{
		// MediatR
		services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProductCommand).Assembly));

		// Add AutoMapper
		services.AddAutoMapper(cfg => { }, typeof(MappingProfile));

		// FluentValidation
		services.AddValidatorsFromAssembly(typeof(CreateProductCommand).Assembly);

		// Pipeline Behaviors
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehaviour<,>));
		services.AddTransient(typeof(IPipelineBehavior<,>), typeof(UnhandledExceptionBehaviour<,>));

		return services;
	}

	public static IServiceCollection AddApiServices(this IServiceCollection services)
	{
		services.AddControllers();
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc("v1", new() { Title = "RogueLearn.User API", Version = "v1" });

			// Include XML comments
			var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
			if (File.Exists(xmlPath))
			{
				c.IncludeXmlComments(xmlPath);
			}

			// --- ADD THIS SECTION TO CONFIGURE JWT AUTHENTICATION IN SWAGGER ---
			c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
			{
				Name = "Authorization",
				Description = "Please enter your JWT with Bearer into field. Example: \"Bearer {token}\"",
				In = ParameterLocation.Header,
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT"
			});

			c.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "Bearer"
						}
					},
					Array.Empty<string>()
				}
			});
			// --- END OF NEW SECTION ---
		});

		services.AddCors(options =>
		{
			options.AddPolicy("AllowAll", builder =>
			{
				builder.AllowAnyOrigin()
					   .AllowAnyMethod()
					   .AllowAnyHeader();
			});
		});

		return services;
	}
}
