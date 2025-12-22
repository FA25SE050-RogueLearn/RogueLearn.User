using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.QuestFeedback.Commands.SubmitQuestStepFeedback;

public class SubmitQuestStepFeedbackCommand : IRequest<Guid>
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }

    [JsonIgnore]
    public Guid QuestId { get; set; }

    [JsonIgnore]
    public Guid StepId { get; set; }

    public int Rating { get; set; } // 1-5
    public string Category { get; set; } = string.Empty;
    public string? Comment { get; set; }
}