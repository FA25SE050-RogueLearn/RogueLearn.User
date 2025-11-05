// RogueLearn.User/src/RogueLearn.User.Application/Features/Onboarding/Queries/GetOnboardingVersionsByProgram/GetOnboardingVersionsByProgramQuery.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Onboarding.Queries.GetOnboardingVersionsByProgram;

public class GetOnboardingVersionsByProgramQuery : IRequest<List<OnboardingVersionDto>>
{
    public Guid ProgramId { get; set; }
}

public class OnboardingVersionDto
{
    public Guid Id { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public int EffectiveYear { get; set; }
}