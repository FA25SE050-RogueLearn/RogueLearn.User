using MassTransit;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Infrastructure.Messaging;

public class MassTransitMessageBus : IMessageBus
{
    private readonly IBus _bus;
    private readonly ILogger<MassTransitMessageBus> _logger;

    public MassTransitMessageBus(IBus bus, ILogger<MassTransitMessageBus> logger)
    {
        _bus = bus;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            _logger.LogInformation("Publishing message of type {MessageType}", typeof(T).Name);

            await _bus.Publish(message, cancellationToken);

            _logger.LogInformation("Message of type {MessageType} published successfully", typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message of type {MessageType}", typeof(T).Name);
            throw;
        }
    }
}