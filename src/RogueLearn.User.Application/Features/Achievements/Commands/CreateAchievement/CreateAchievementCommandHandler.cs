using AutoMapper;
using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Achievements.Commands.CreateAchievement;

public class CreateAchievementCommandHandler : IRequestHandler<CreateAchievementCommand, CreateAchievementResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<CreateAchievementCommand> _validator;

    public CreateAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        IMapper mapper,
        IValidator<CreateAchievementCommand> validator)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<CreateAchievementResponse> Handle(CreateAchievementCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        // Uniqueness check by key
        var keyExists = await _achievementRepository.AnyAsync(a => a.Key == request.Key, cancellationToken);
        if (keyExists)
        {
            throw new ConflictException($"Achievement with key '{request.Key}' already exists.");
        }

        Dictionary<string, object>? ruleConfigDict = null;
        if (!string.IsNullOrWhiteSpace(request.RuleConfig))
        {
            // Parse JSON string into a dictionary to support JSONB column
            ruleConfigDict = JsonSerializer.Deserialize<Dictionary<string, object>>(request.RuleConfig.Trim());
        }

        var entity = new Achievement
        {
            Key = request.Key.Trim(),
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            RuleType = string.IsNullOrWhiteSpace(request.RuleType) ? null : request.RuleType.Trim(),
            RuleConfig = ruleConfigDict,
            Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim(),
            Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim(),
            IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl,
            Version = request.Version <= 0 ? 1 : request.Version,
            IsActive = request.IsActive,
            SourceService = request.SourceService.Trim()
        };

        var created = await _achievementRepository.AddAsync(entity, cancellationToken);
        return _mapper.Map<CreateAchievementResponse>(created);
    }
}