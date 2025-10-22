using AutoMapper;
using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

/// <summary>
/// Handles updating an existing Achievement.
/// - Validates input via FluentValidation.
/// - Ensures key uniqueness when changed.
/// - Parses RuleConfig JSON safely with validation error reporting.
/// - Emits structured logs for observability.
/// </summary>
public class UpdateAchievementCommandHandler : IRequestHandler<UpdateAchievementCommand, UpdateAchievementResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateAchievementCommand> _validator;
    private readonly ILogger<UpdateAchievementCommandHandler> _logger;

    public UpdateAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        IMapper mapper,
        IValidator<UpdateAchievementCommand> validator,
        ILogger<UpdateAchievementCommandHandler> logger)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Updates an achievement and persists changes.
    /// </summary>
    public async Task<UpdateAchievementResponse> Handle(UpdateAchievementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling UpdateAchievementCommand for Id={AchievementId}, Key={Key}", request.Id, request.Key);

        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _achievementRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
        {
            _logger.LogWarning("Achievement not found: Id={AchievementId}", request.Id);
            throw new NotFoundException("Achievement", request.Id);
        }

        // Uniqueness check: if key changes and another entity already has it
        var trimmedKey = request.Key.Trim();
        var keyChanged = !string.Equals(entity.Key, trimmedKey, StringComparison.Ordinal);
        if (keyChanged)
        {
            var exists = await _achievementRepository.AnyAsync(a => a.Key == trimmedKey && a.Id != request.Id, cancellationToken);
            if (exists)
            {
                _logger.LogWarning("Achievement key conflict on update: Id={AchievementId}, NewKey={Key}", request.Id, trimmedKey);
                throw new ConflictException($"Achievement with key '{trimmedKey}' already exists.");
            }
        }

        entity.Key = trimmedKey;
        entity.Name = request.Name.Trim();
        entity.Description = request.Description.Trim();
        entity.RuleType = string.IsNullOrWhiteSpace(request.RuleType) ? null : request.RuleType.Trim();

        if (string.IsNullOrWhiteSpace(request.RuleConfig))
        {
            entity.RuleConfig = null;
        }
        else
        {
            try
            {
                entity.RuleConfig = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(request.RuleConfig.Trim());
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse RuleConfig JSON for Id={AchievementId}", request.Id);
                throw new RogueLearn.User.Application.Exceptions.ValidationException(new [] { new FluentValidation.Results.ValidationFailure("RuleConfig", "RuleConfig must be a valid JSON object.") });
            }
        }

        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        entity.IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl;
        entity.Version = request.Version <= 0 ? 1 : request.Version;
        entity.IsActive = request.IsActive;
        entity.SourceService = request.SourceService.Trim();

        var updated = await _achievementRepository.UpdateAsync(entity, cancellationToken);
        _logger.LogInformation("Updated achievement: Id={AchievementId}, Key={Key}", updated.Id, updated.Key);
        return _mapper.Map<UpdateAchievementResponse>(updated);
    }
}