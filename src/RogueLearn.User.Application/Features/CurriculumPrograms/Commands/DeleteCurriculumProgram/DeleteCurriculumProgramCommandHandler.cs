using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.CurriculumPrograms.Commands.DeleteCurriculumProgram;

public class DeleteCurriculumProgramCommandHandler : IRequestHandler<DeleteCurriculumProgramCommand>
{
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;

    public DeleteCurriculumProgramCommandHandler(ICurriculumProgramRepository curriculumProgramRepository)
    {
        _curriculumProgramRepository = curriculumProgramRepository;
    }

    public async Task Handle(DeleteCurriculumProgramCommand request, CancellationToken cancellationToken)
    {
        var program = await _curriculumProgramRepository.GetByIdAsync(request.Id, cancellationToken);
        if (program == null)
        {
            throw new NotFoundException("CurriculumProgram", request.Id);
        }

        await _curriculumProgramRepository.DeleteAsync(request.Id, cancellationToken);
    }
}