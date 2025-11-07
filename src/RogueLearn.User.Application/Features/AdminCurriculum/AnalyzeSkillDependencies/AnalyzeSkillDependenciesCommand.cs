// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/AnalyzeSkillDependencies/AnalyzeSkillDependenciesCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.AdminCurriculum.AnalyzeSkillDependencies;

public class AnalyzeSkillDependenciesCommand : IRequest<AnalyzeSkillDependenciesResponse>
{
    public Guid CurriculumVersionId { get; set; }
}
