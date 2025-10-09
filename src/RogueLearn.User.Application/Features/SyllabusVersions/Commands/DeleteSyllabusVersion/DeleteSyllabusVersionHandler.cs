using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.DeleteSyllabusVersion;

public class DeleteSyllabusVersionHandler : IRequestHandler<DeleteSyllabusVersionCommand>
{
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;

    public DeleteSyllabusVersionHandler(ISyllabusVersionRepository syllabusVersionRepository)
    {
        _syllabusVersionRepository = syllabusVersionRepository;
    }

    public async Task Handle(DeleteSyllabusVersionCommand request, CancellationToken cancellationToken)
    {
        var syllabusVersion = await _syllabusVersionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (syllabusVersion == null)
        {
            throw new NotFoundException("SyllabusVersion", request.Id);
        }

        await _syllabusVersionRepository.DeleteAsync(syllabusVersion.Id, cancellationToken);
    }
}