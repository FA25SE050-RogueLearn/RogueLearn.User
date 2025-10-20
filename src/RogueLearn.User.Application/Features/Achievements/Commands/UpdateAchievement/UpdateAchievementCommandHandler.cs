using AutoMapper;
using FluentValidation;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Achievements.Commands.UpdateAchievement;

public class UpdateAchievementCommandHandler : IRequestHandler<UpdateAchievementCommand, UpdateAchievementResponse>
{
    private readonly IAchievementRepository _achievementRepository;
    private readonly IMapper _mapper;
    private readonly IValidator<UpdateAchievementCommand> _validator;

    public UpdateAchievementCommandHandler(
        IAchievementRepository achievementRepository,
        IMapper mapper,
        IValidator<UpdateAchievementCommand> validator)
    {
        _achievementRepository = achievementRepository;
        _mapper = mapper;
        _validator = validator;
    }

    public async Task<UpdateAchievementResponse> Handle(UpdateAchievementCommand request, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(request, cancellationToken);

        var entity = await _achievementRepository.GetByIdAsync(request.Id, cancellationToken);
        if (entity == null)
        {
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
            entity.RuleConfig = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(request.RuleConfig.Trim());
        }

        entity.Category = string.IsNullOrWhiteSpace(request.Category) ? null : request.Category.Trim();
        entity.Icon = string.IsNullOrWhiteSpace(request.Icon) ? null : request.Icon.Trim();
        entity.IconUrl = string.IsNullOrWhiteSpace(request.IconUrl) ? null : request.IconUrl;
        entity.Version = request.Version <= 0 ? 1 : request.Version;
        entity.IsActive = request.IsActive;
        entity.SourceService = request.SourceService.Trim();

        var updated = await _achievementRepository.UpdateAsync(entity, cancellationToken);
        return _mapper.Map<UpdateAchievementResponse>(updated);
    }
}