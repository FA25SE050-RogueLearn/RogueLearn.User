// RogueLearn.User/src/RogueLearn.User.Application/Features/SyllabusVersions/Commands/UpdateSyllabusVersion/UpdateSyllabusVersionHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json; // ADDED: To handle JSON deserialization.

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

        // MODIFIED: Deserialize the incoming JSON string from the command into the Dictionary
        // required by the domain entity. A null/empty string results in a null dictionary.
        syllabusVersion.Content = !string.IsNullOrWhiteSpace(request.Content)
            ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.Content)
            : null;
        syllabusVersion.EffectiveDate = request.EffectiveDate;
        syllabusVersion.IsActive = request.IsActive;

        var updated = await _syllabusVersionRepository.UpdateAsync(syllabusVersion, cancellationToken);
        return _mapper.Map<UpdateSyllabusVersionResponse>(updated);
    }
}