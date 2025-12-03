using AutoMapper;
using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;

/// <summary>
/// Handles creation of a new Achievement.
/// - Validates input via FluentValidation.
/// - Ensures uniqueness by key.
/// - Parses RuleConfig JSON into a dictionary for persistence.
/// - Emits structured logs for observability.
/// </summary>
public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, CreateAchievementResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAchievementCommand> _validator;
    private readonly ILogger<CreateAchievementCommandHandler> _logger;

    public CreateAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        IMapper mapper,
        IValidator<CreateAchievementCommand> validator,
        ILogger<CreateAchievementCommandHandler> logger)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>
    /// Creates an achievement and persists it.
    /// </summary>
    public async Task<CreateAchievementResponse> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling CreateAchievementCommand for Key={Key}, Name={Name}", request.Key, request.Name);

        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Uniqueness check by key
        var keyExists = await _achievementRepository.AnyAsync(a => a.Key == request.Key, cancellationToken);
        if (keyExists)
        {
            _logger.LogWarning("Achievement key conflict: Key={Key}", request.Key);
            throw new ConflictException($"Achievement with key '{request.Key}' already exists.");
        }

        Dictionary<string, object>? ruleConfigDict = null;
        if (!string.IsNullOrWhiteSpace(request.RuleConfig))
        {
            try
            {
                // Parse JSON string into a dictionary to support JSONB column
                ruleConfigDict = JsonSerializer.Deserialize<Dictionary<string, object>>(request.RuleConfig.Trim());
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse RuleConfig JSON for Key={Key}", request.Key);
                throw new RogueLearn.User.Application.Exceptions.ValidationException(new [] { new FluentValidation.Results.ValidationFailure("RuleConfig", "RuleConfig must be a valid JSON object.") });
            }
        }

        var entity = new Achievement
        {
            Key = request.Key.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            RuleType = string.IsNullOrWhiteSpace(request.RuleType) ? null : request.RuleType.Trim(),
            RuleConfig = ruleConfigDict,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl,
            Version = request.Version <= 0 ? 1 : request.Version,
            IsActive = request.IsActive,
            SourceService = request.SourceService.Trim()
        };

        var created = await _achievementRepository.AddAsync(entity, cancellationToken);
        _logger.LogInformation("Created achievement: Id={AchievementId}, Key={Key}", created.Id, created.Key);
        return _mapper.Map<CreateAchievementResponse>(created);
    }
}