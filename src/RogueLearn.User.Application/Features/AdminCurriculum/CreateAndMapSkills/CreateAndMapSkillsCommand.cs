// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/CreateAndMapSkills/CreateAndMapSkillsCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.AdminCurriculum.CreateAndMapSkills;

public class CreateAndMapSkillsCommand : IRequest<CreateAndMapSkillsResponse>
{
    public Guid SyllabusVersionId { get; set; }
}