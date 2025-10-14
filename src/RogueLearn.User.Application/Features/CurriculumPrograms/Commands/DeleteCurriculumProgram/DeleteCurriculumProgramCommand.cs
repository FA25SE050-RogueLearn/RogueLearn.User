using MediatR;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;

public class DeleteCurriculumProgramCommand : IRequest
{
    public Guid Id { get; set; }
}