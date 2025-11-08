// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Queries/GetSubjectSkillMappings/GetSubjectSkillMappingsQueryHandler.cs
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class GetSubjectSkillMappingsQueryHandler : IRequestHandler<GetSubjectSkillMappingsQuery, List<SubjectSkillMappingDto>>
{
    private readonly ISubjectSkillMappingRepository _repository;
    private readonly ISkillRepository _skillRepository;

    public GetSubjectSkillMappingsQueryHandler(ISubjectSkillMappingRepository repository, ISkillRepository skillRepository)
    {
        _repository = repository;
        _skillRepository = skillRepository;
    }

    public async Task<List<SubjectSkillMappingDto>> Handle(GetSubjectSkillMappingsQuery request, CancellationToken cancellationToken)
    {
        var mappings = await _repository.FindAsync(m => m.SubjectId == request.SubjectId, cancellationToken);
        var skillIds = mappings.Select(m => m.SkillId).ToList();
        var skills = (await _skillRepository.GetAllAsync(cancellationToken)).Where(s => skillIds.Contains(s.Id)).ToDictionary(s => s.Id);

        return mappings.Select(m => new SubjectSkillMappingDto
        {
            Id = m.Id,
            SubjectId = m.SubjectId,
            SkillId = m.SkillId,
            SkillName = skills.TryGetValue(m.SkillId, out var skill) ? skill.Name : "Unknown Skill",
            RelevanceWeight = m.RelevanceWeight,
            CreatedAt = m.CreatedAt
        }).ToList();
    }
}