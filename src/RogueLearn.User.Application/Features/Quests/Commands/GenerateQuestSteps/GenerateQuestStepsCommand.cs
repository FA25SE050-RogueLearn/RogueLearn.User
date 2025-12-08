// RogueLearn.User/src/RogueLearn.User.Application/Features/Quests/Commands/GenerateQuestSteps/GenerateQuestStepsCommand.cs
using MediatR;
using RogueLearn.User.Domain.Entities;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;

public class GenerateQuestStepsCommand : IRequest<List<GeneratedQuestStepDto>>
{
    // Changed from AuthUserId (student) to AdminId (creator)
    [JsonIgnore]
    public Guid AdminId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    /// <summary>
    /// Hangfire background job ID for progress tracking.
    /// Allows the handler to update real-time progress in the job parameters.
    /// </summary>
    [JsonIgnore]
    public string HangfireJobId { get; set; } = string.Empty;
}