using BuildingBlocks.Shared.Interfaces;
using BuildingBlocks.Shared.Repositories;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Infrastructure.Messaging;
using RogueLearn.User.Infrastructure.Persistence;
using Supabase;

namespace RogueLearn.User.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Supabase
        var supabaseUrl = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL is not configured");
        var supabaseKey = configuration["Supabase:ApiKey"] ?? throw new InvalidOperationException("Supabase API Key is not configured");

        services.AddSingleton(provider =>
        {
            var options = new SupabaseOptions
            {
                AutoConnectRealtime = true
            };
            return new Client(supabaseUrl, supabaseKey, options);
        });

        // Configure MassTransit with RabbitMQ
        services.AddMassTransit(busConfig =>
        {
            busConfig.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
                var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "guest";
                var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "guest";

                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        // Register repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();

        // Register message bus
        services.AddScoped<IMessageBus, MassTransitMessageBus>();

        return services;
    }
}