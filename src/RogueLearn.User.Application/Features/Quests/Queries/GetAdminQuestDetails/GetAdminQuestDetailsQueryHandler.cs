using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Common; // For JSON converter if needed
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetAdminQuestDetails;

public class GetAdminQuestDetailsQueryHandler : IRequestHandler<GetAdminQuestDetailsQuery, AdminQuestDetailsDto?>
{
    private readonly IQuestRepository _questRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ILogger<GetAdminQuestDetailsQueryHandler> _logger;

    public GetAdminQuestDetailsQueryHandler(
        IQuestRepository questRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        ILogger<GetAdminQuestDetailsQueryHandler> logger)
    {
        _questRepository = questRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _logger = logger;
    }

    public async Task<AdminQuestDetailsDto?> Handle(GetAdminQuestDetailsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching admin details for Quest {QuestId}", request.QuestId);

        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null) return null;

        var dto = new AdminQuestDetailsDto
        {
            Id = quest.Id,
            Title = quest.Title,
            Description = quest.Description,
            QuestType = quest.QuestType.ToString(),
            // Removed DifficultyLevel mapping
            Status = quest.Status.ToString(),
            IsActive = quest.IsActive
        };

        if (quest.SubjectId.HasValue)
        {
            var subject = await _subjectRepository.GetByIdAsync(quest.SubjectId.Value, cancellationToken);
            if (subject != null)
            {
                dto.SubjectCode = subject.SubjectCode;
                dto.SubjectName = subject.SubjectName;
            }
        }

        var steps = await _questStepRepository.GetByQuestIdAsync(request.QuestId, cancellationToken);

        foreach (var step in steps.OrderBy(s => s.StepNumber))
        {
            var stepDto = new AdminQuestStepDto
            {
                Id = step.Id,
                StepNumber = step.StepNumber,
                ModuleNumber = step.ModuleNumber,
                Title = step.Title,
                Description = step.Description,
                ExperiencePoints = step.ExperiencePoints,
                DifficultyVariant = step.DifficultyVariant,
                CreatedAt = step.CreatedAt,
                Content = ParseContent(step.Content)
            };

            if (string.Equals(step.DifficultyVariant, "Standard", StringComparison.OrdinalIgnoreCase))
            {
                dto.StandardSteps.Add(stepDto);
            }
            else if (string.Equals(step.DifficultyVariant, "Supportive", StringComparison.OrdinalIgnoreCase))
            {
                dto.SupportiveSteps.Add(stepDto);
            }
            else if (string.Equals(step.DifficultyVariant, "Challenging", StringComparison.OrdinalIgnoreCase))
            {
                dto.ChallengingSteps.Add(stepDto);
            }
        }

        return dto;
    }

    private static object? ParseContent(object? content)
    {
        if (content is null) return null;
        if (content is string s)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                // Use a basic converter or just return deserialized object
                return JsonSerializer.Deserialize<object>(s, options);
            }
            catch
            {
                return s;
            }
        }
        return content;
    }
}