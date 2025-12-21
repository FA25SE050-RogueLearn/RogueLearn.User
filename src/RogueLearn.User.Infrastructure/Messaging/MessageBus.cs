using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Events;
using System.Text.Json;

namespace RogueLearn.User.Infrastructure.Messaging;

public class MessageBus : IMessageBus
{
    private readonly ILogger<MessageBus> _logger;

    public MessageBus(ILogger<MessageBus> logger)
    {
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serializedMessage = JsonSerializer.Serialize(message, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            _logger.LogInformation("Publishing message of type {MessageType}: {Message}", 
                typeof(T).Name, serializedMessage);

            await Task.Delay(10, cancellationToken);

            _logger.LogInformation("Message published successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }
}