// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/SuggestSkillsFromSyllabus/SuggestSkillsFromSyllabusCommand.cs
using MediatR;

namespace RogueLearn.User.Application.Features.AdminCurriculum.SuggestSkillsFromSyllabus;

public class SuggestSkillsFromSyllabusCommand : IRequest<SuggestSkillsFromSyllabusResponse>
{
    public Guid SyllabusVersionId { get; set; }
}