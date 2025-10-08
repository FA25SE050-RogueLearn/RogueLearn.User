// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Infrastructure/Extensions/ServiceCollectionExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RogueLearn.Quests.Application.Interfaces;
using RogueLearn.Quests.Domain.Interfaces;
using RogueLearn.Quests.Infrastructure.HttpClients;
using RogueLearn.Quests.Infrastructure.Persistence;

namespace RogueLearn.Quests.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddQuestsInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Register Repositories
		services.AddScoped<IQuestRepository, QuestRepository>();
		// Add other repositories here as you create them...

		// Register HttpClients for inter-service communication
		services.AddHttpClient<IUserServiceClient, UserServiceClient>(client =>
		{
			var baseUrl = configuration["UserService:BaseUrl"]
				?? throw new InvalidOperationException("UserService BaseUrl is not configured.");
			client.BaseAddress = new Uri(baseUrl);
		});

		return services;
	}
}