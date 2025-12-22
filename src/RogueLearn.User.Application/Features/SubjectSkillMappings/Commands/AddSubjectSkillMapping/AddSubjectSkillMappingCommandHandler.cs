using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;

public class AddSubjectSkillMappingCommandHandler : IRequestHandler<AddSubjectSkillMappingCommand, AddSubjectSkillMappingResponse>
{
    private readonly ISubjectSkillMappingRepository _mappingRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<AddSubjectSkillMappingCommandHandler> _logger;

    public AddSubjectSkillMappingCommandHandler(
        ISubjectSkillMappingRepository mappingRepository,
        ISubjectRepository subjectRepository,
        ISkillRepository skillRepository,
        ILogger<AddSubjectSkillMappingCommandHandler> logger)
    {
        _mappingRepository = mappingRepository;
        _subjectRepository = subjectRepository;
        _skillRepository = skillRepository;
        _logger = logger;
    }

    public async Task<AddSubjectSkillMappingResponse> Handle(AddSubjectSkillMappingCommand request, CancellationToken cancellationToken)
    {
        // Validate that both the subject and skill exist before creating a mapping
        if (!await _subjectRepository.ExistsAsync(request.SubjectId, cancellationToken))
        {
            throw new NotFoundException(nameof(Subject), request.SubjectId);
        }
        if (!await _skillRepository.ExistsAsync(request.SkillId, cancellationToken))
        {
            throw new NotFoundException(nameof(Skill), request.SkillId);
        }

        // Prevent duplicate mappings
        var existing = await _mappingRepository.FirstOrDefaultAsync(m => m.SubjectId == request.SubjectId && m.SkillId == request.SkillId, cancellationToken);
        if (existing != null)
        {
            throw new ConflictException("This skill is already mapped to this subject.");
        }

        var newMapping = new SubjectSkillMapping
        {
            SubjectId = request.SubjectId,
            SkillId = request.SkillId,
            RelevanceWeight = request.RelevanceWeight
        };

        var createdMapping = await _mappingRepository.AddAsync(newMapping, cancellationToken);

        _logger.LogInformation("Created Subject-Skill mapping for SubjectId={SubjectId}, SkillId={SkillId}", request.SubjectId, request.SkillId);

        return new AddSubjectSkillMappingResponse
        {
            Id = createdMapping.Id,
            SubjectId = createdMapping.SubjectId,
            SkillId = createdMapping.SkillId,
            RelevanceWeight = createdMapping.RelevanceWeight,
            CreatedAt = createdMapping.CreatedAt
        };
    }
}