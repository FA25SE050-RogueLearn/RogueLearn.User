// RogueLearn.User/src/RogueLearn.User.Application/Features/SubjectSkillMappings/Queries/GetSubjectSkillMappings/GetSubjectSkillMappingsQueryHandler.cs
using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.SubjectSkillMappings.Queries.GetSubjectSkillMappings;

public class GetSubjectSkillMappingsQueryHandler : IRequestHandler<GetSubjectSkillMappingsQuery, List<SubjectSkillMappingDto>>
{
    private readonly ISubjectSkillMappingRepository _repository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository; // NEW Dependency

    public GetSubjectSkillMappingsQueryHandler(
        ISubjectSkillMappingRepository repository,
        ISkillRepository skillRepository,
        ISkillDependencyRepository skillDependencyRepository)
    {
        _repository = repository;
        _skillRepository = skillRepository;
        _skillDependencyRepository = skillDependencyRepository;
    }

    public async Task<List<SubjectSkillMappingDto>> Handle(GetSubjectSkillMappingsQuery request, CancellationToken cancellationToken)
    {
        // 1. Get all mappings for the subject
        var mappings = await _repository.FindAsync(m => m.SubjectId == request.SubjectId, cancellationToken);
        if (!mappings.Any()) return new List<SubjectSkillMappingDto>();

        // 2. Get details for all mapped skills
        var skillIds = mappings.Select(m => m.SkillId).Distinct().ToList();
        var skills = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => skillIds.Contains(s.Id))
            .ToDictionary(s => s.Id);

        // 3. Get all dependencies for the relevant skills
        // We fetch all dependencies where the dependent skill (SkillId) is in our list.
        // Note: Ideally we'd filter in DB, but GenericRepository usually supports simple predicates.
        // Fetching all might be heavy if table is huge, but for now we filter in memory.
        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);
        var relevantDependencies = allDependencies
            .Where(d => skillIds.Contains(d.SkillId))
            .GroupBy(d => d.SkillId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 4. Fetch names of prerequisite skills (some might not be in the current subject map)
        var prerequisiteIds = relevantDependencies.Values
            .SelectMany(list => list.Select(d => d.PrerequisiteSkillId))
            .Distinct()
            .ToList();

        // We need names for prerequisites that might NOT be in the current 'skills' dictionary
        var extraSkillIds = prerequisiteIds.Except(skillIds).ToList();
        var extraSkills = new Dictionary<Guid, string>();

        if (extraSkillIds.Any())
        {
            // Efficiently fetch only missing prerequisite skills
            // Note: Assuming GetAllAsync caches or is fast enough, otherwise use GetByIds if available
            var additionalSkills = (await _skillRepository.GetAllAsync(cancellationToken))
               .Where(s => extraSkillIds.Contains(s.Id))
               .ToList();

            foreach (var s in additionalSkills) extraSkills[s.Id] = s.Name;
        }

        // 5. Build the response
        return mappings.Select(m => {
            var dto = new SubjectSkillMappingDto
            {
                Id = m.Id,
                SubjectId = m.SubjectId,
                SkillId = m.SkillId,
                SkillName = skills.TryGetValue(m.SkillId, out var skill) ? skill.Name : "Unknown Skill",
                RelevanceWeight = m.RelevanceWeight,
                CreatedAt = m.CreatedAt
            };

            if (relevantDependencies.TryGetValue(m.SkillId, out var deps))
            {
                dto.Prerequisites = deps.Select(d => new PrerequisiteDto
                {
                    PrerequisiteSkillId = d.PrerequisiteSkillId,
                    // Try finding name in 'skills' (mapped to subject) first, then 'extraSkills' (external deps)
                    PrerequisiteSkillName = skills.TryGetValue(d.PrerequisiteSkillId, out var ps)
                        ? ps.Name
                        : (extraSkills.TryGetValue(d.PrerequisiteSkillId, out var esName) ? esName : "Unknown")
                }).ToList();
            }

            return dto;
        }).ToList();
    }
}