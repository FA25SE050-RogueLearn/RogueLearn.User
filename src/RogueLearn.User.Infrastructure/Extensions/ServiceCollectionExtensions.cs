using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Messaging;
using RogueLearn.User.Infrastructure.Persistence;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
  public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
  {
    // Register Supabase Client as Scoped per request to attach JWT header
    services.AddScoped<Client>(sp =>
    {
      var cfg = sp.GetRequiredService<IConfiguration>();
      var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

      var supabaseUrl = cfg["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL is not configured");
      var supabaseKey = cfg["Supabase:ApiKey"] ?? throw new InvalidOperationException("Supabase API Key is not configured");

      var options = new SupabaseOptions
      {
        AutoConnectRealtime = true,
        Headers = new Dictionary<string, string>()
      };

      // Forward the incoming Authorization header to Supabase
      var authHeader = httpContextAccessor.HttpContext?.Request?.Headers["Authorization"].ToString();
      if (!string.IsNullOrWhiteSpace(authHeader))
      {
        options.Headers["Authorization"] = authHeader;
      }

      var client = new Client(supabaseUrl, supabaseKey, options);
      // Initialize synchronously for scoped lifetime
      client.InitializeAsync().GetAwaiter().GetResult();
      return client;
    });

    // Configure MassTransit with RabbitMQ
    //services.AddMassTransit(busConfig =>
    //{
    //    busConfig.UsingRabbitMq((context, cfg) =>
    //    {
    //        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
    //        var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "guest";
    //        var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";

    //        cfg.Host(rabbitMqHost, "/", h =>
    //        {
    //            h.Username(rabbitMqUsername);
    //            h.Password(rabbitMqPassword);
    //        });

    //        cfg.ConfigureEndpoints(context);
    //    });
    //});

    // Register Generic Repository
    services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

    // Register Specific Repositories - ADD ALL YOUR REPOSITORIES HERE
    services.AddScoped<IUserProfileRepository, UserProfileRepository>();
    services.AddScoped<IRoleRepository, RoleRepository>();
    services.AddScoped<IUserRoleRepository, UserRoleRepository>();
    // Curriculum repositories
    services.AddScoped<ICurriculumProgramRepository, CurriculumProgramRepository>();
    services.AddScoped<ICurriculumVersionRepository, CurriculumVersionRepository>();
    services.AddScoped<ICurriculumVersionActivationRepository, CurriculumVersionActivationRepository>();
    services.AddScoped<ICurriculumStructureRepository, CurriculumStructureRepository>();
    services.AddScoped<ISyllabusVersionRepository, SyllabusVersionRepository>();
    services.AddScoped<ICurriculumImportJobRepository, CurriculumImportJobRepository>();

    // Register Message Bus
    //services.AddScoped<IMessageBus, MassTransitMessageBus>();

    return services;
  }
}