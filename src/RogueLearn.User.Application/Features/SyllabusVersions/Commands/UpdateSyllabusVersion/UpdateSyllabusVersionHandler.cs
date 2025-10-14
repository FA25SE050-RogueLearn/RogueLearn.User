using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;

public class UpdateSyllabusVersionHandler : IRequestHandler<UpdateSyllabusVersionCommand, UpdateSyllabusVersionResponse>
{
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly IMapper _mapper;

    public UpdateSyllabusVersionHandler(
        ISyllabusVersionRepository syllabusVersionRepository,
        IMapper mapper)
    {
        _syllabusVersionRepository = syllabusVersionRepository;
        _mapper = mapper;
    }

    public async Task<UpdateSyllabusVersionResponse> Handle(UpdateSyllabusVersionCommand request, CancellationToken cancellationToken)
    {
        var syllabusVersion = await _syllabusVersionRepository.GetByIdAsync(request.Id, cancellationToken);
        if (syllabusVersion == null)
        {
            throw new NotFoundException("SyllabusVersion", request.Id);
        }

        syllabusVersion.Content = request.Content;
        syllabusVersion.EffectiveDate = request.EffectiveDate;
        syllabusVersion.IsActive = request.IsActive;

        var updated = await _syllabusVersionRepository.UpdateAsync(syllabusVersion, cancellationToken);
        return _mapper.Map<UpdateSyllabusVersionResponse>(updated);
    }
}