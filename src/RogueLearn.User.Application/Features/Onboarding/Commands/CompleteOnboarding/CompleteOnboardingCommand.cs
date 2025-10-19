// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Commands/CompleteOnboarding/CompleteOnboardingCommand.cs
using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    public Guid CurriculumVersionId { get; set; } // Represents the chosen "Route"
    public Guid CareerRoadmapId { get; set; } // Represents the chosen "Class"
}