// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/EstablishSkillDependencies/EstablishSkillDependenciesCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.Student.Commands.EstablishSkillDependencies;

public class EstablishSkillDependenciesCommand : IRequest<EstablishSkillDependenciesResponse>
{
    public Guid AuthUserId { get; set; }
    public Guid CurriculumVersionId { get; set; }
}