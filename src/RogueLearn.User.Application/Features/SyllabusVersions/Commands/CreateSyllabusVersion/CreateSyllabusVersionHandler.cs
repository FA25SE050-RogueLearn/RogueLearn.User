// RogueLearn.User/src/RogueLearn.User.Application/Features/SyllabusVersions/Commands/CreateSyllabusVersion/CreateSyllabusVersionHandler.cs
using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json; // ADDED: To handle JSON deserialization.

namespace RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;

public class CreateSyllabusVersionHandler : IRequestHandler<CreateSyllabusVersionCommand, CreateSyllabusVersionResponse>
{
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IMapper _mapper;

    public CreateSyllabusVersionHandler(
        ISyllabusVersionRepository syllabusVersionRepository,
        ISubjectRepository subjectRepository,
        IMapper mapper)
    {
        _syllabusVersionRepository = syllabusVersionRepository;
        _subjectRepository = subjectRepository;
        _mapper = mapper;
    }

    public async Task<CreateSyllabusVersionResponse> Handle(CreateSyllabusVersionCommand request, CancellationToken cancellationToken)
    {
        // Validate subject exists
        var subject = await _subjectRepository.GetByIdAsync(request.SubjectId, cancellationToken);
        if (subject == null)
        {
            throw new NotFoundException("Subject", request.SubjectId);
        }

        // Check if version number already exists for this subject
        var existingVersions = await _syllabusVersionRepository.FindAsync(
            sv => sv.SubjectId == request.SubjectId && sv.VersionNumber == request.VersionNumber,
            cancellationToken);

        if (existingVersions.Any())
        {
            // Duplicate version for this subject is a resource conflict
            throw new ConflictException($"Version {request.VersionNumber} already exists for this subject");
        }

        var syllabusVersion = new SyllabusVersion
        {
            SubjectId = request.SubjectId,
            VersionNumber = request.VersionNumber,
            // MODIFIED: Deserialize the incoming JSON string from the command into the Dictionary
            // required by the domain entity. A null/empty string results in a null dictionary.
            Content = !string.IsNullOrWhiteSpace(request.Content)
                ? JsonSerializer.Deserialize<Dictionary<string, object>>(request.Content)
                : null,
            EffectiveDate = request.EffectiveDate,
            IsActive = request.IsActive,
            CreatedBy = request.CreatedBy
        };

        var created = await _syllabusVersionRepository.AddAsync(syllabusVersion, cancellationToken);
        return _mapper.Map<CreateSyllabusVersionResponse>(created);
    }
}