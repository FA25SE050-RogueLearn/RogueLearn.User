using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace RogueLearn.User.Application.Features.Quests.Commands.UpdateQuestStepContent;

public class UpdateQuestStepContentCommandHandler : IRequestHandler<UpdateQuestStepContentCommand, UpdateQuestStepContentResponse>
{
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ILogger<UpdateQuestStepContentCommandHandler> _logger;

    public UpdateQuestStepContentCommandHandler(
        IQuestStepRepository questStepRepository,
        ILogger<UpdateQuestStepContentCommandHandler> logger)
    {
        _questStepRepository = questStepRepository;
        _logger = logger;
    }

    public async Task<UpdateQuestStepContentResponse> Handle(UpdateQuestStepContentCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating content for QuestStep {QuestStepId}", request.QuestStepId);

        var questStep = await _questStepRepository.GetByIdAsync(request.QuestStepId, cancellationToken);
        if (questStep == null)
        {
            throw new NotFoundException("QuestStep", request.QuestStepId);
        }

        // Construct the root object structure expected by the system
        var contentDict = new Dictionary<string, object>
        {
            { "activities", request.Activities }
        };

        // Serialize to JSON string to ensure clean format, then deserialize back to Dictionary<string, object>
        // for the Supabase client, which handles JObjects/JArrays best when passed as Dictionaries.
        var jsonString = JsonConvert.SerializeObject(contentDict);
        var safeContent = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

        questStep.Content = safeContent;
        questStep.UpdatedAt = DateTimeOffset.UtcNow;

        await _questStepRepository.UpdateAsync(questStep, cancellationToken);

        _logger.LogInformation("Successfully updated content for QuestStep {QuestStepId}. Activity count: {Count}",
            request.QuestStepId, request.Activities.Count);

        return new UpdateQuestStepContentResponse
        {
            QuestStepId = questStep.Id,
            IsSuccess = true,
            Message = "Quest step content updated successfully.",
            ActivityCount = request.Activities.Count,
            UpdatedAt = questStep.UpdatedAt
        };
    }
}