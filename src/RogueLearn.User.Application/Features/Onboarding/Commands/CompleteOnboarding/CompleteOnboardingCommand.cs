using MediatR;
using System.Text.Json.Serialization;

namespace RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommand : IRequest
{
    [JsonIgnore]
    public Guid AuthUserId { get; set; }
    public Guid CurriculumProgramId { get; set; }
    public Guid CareerRoadmapId { get; set; }
}