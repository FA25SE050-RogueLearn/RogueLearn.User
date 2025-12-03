using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Quests.Queries.GetQuestSkills;

public class GetQuestSkillsQueryHandler : IRequestHandler<GetQuestSkillsQuery, GetQuestSkillsResponse?>
{
    private readonly IQuestRepository _questRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;

    public GetQuestSkillsQueryHandler(
        IQuestRepository questRepository,
        ISubjectRepository subjectRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        ISkillRepository skillRepository,
        ISkillDependencyRepository skillDependencyRepository)
    {
        _questRepository = questRepository;
        _subjectRepository = subjectRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _skillRepository = skillRepository;
        _skillDependencyRepository = skillDependencyRepository;
    }

    public async Task<GetQuestSkillsResponse?> Handle(GetQuestSkillsQuery request, CancellationToken cancellationToken)
    {
        var quest = await _questRepository.GetByIdAsync(request.QuestId, cancellationToken);
        if (quest == null)
            return null;

        var response = new GetQuestSkillsResponse
        {
            QuestId = quest.Id,
            SubjectId = quest.SubjectId
        };

        if (quest.SubjectId == null)
            return response;

        var subject = await _subjectRepository.GetByIdAsync(quest.SubjectId.Value, cancellationToken);
        if (subject != null)
            response.SubjectName = subject.SubjectName;

        var mappings = await _subjectSkillMappingRepository.FindAsync(
            m => m.SubjectId == quest.SubjectId.Value, cancellationToken);

        if (!mappings.Any())
            return response;

        var skillIds = mappings.Select(m => m.SkillId).Distinct().ToList();
        var skills = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => skillIds.Contains(s.Id))
            .ToDictionary(s => s.Id);

        var allDependencies = await _skillDependencyRepository.GetAllAsync(cancellationToken);
        var relevantDependencies = allDependencies
            .Where(d => skillIds.Contains(d.SkillId))
            .GroupBy(d => d.SkillId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var prerequisiteIds = relevantDependencies.Values
            .SelectMany(list => list.Select(d => d.PrerequisiteSkillId))
            .Distinct()
            .ToList();

        var extraSkillIds = prerequisiteIds.Except(skillIds).ToList();
        var extraSkills = new Dictionary<Guid, string>();

        if (extraSkillIds.Any())
        {
            var additionalSkills = (await _skillRepository.GetAllAsync(cancellationToken))
                .Where(s => extraSkillIds.Contains(s.Id))
                .ToList();

            foreach (var s in additionalSkills)
                extraSkills[s.Id] = s.Name;
        }

        response.Skills = mappings.Select(m =>
        {
            var dto = new QuestSkillDto
            {
                SkillId = m.SkillId,
                SkillName = skills.TryGetValue(m.SkillId, out var skill) ? skill.Name : "Unknown Skill",
                Domain = skills.TryGetValue(m.SkillId, out var s) ? s.Domain : null,
                RelevanceWeight = m.RelevanceWeight
            };

            if (relevantDependencies.TryGetValue(m.SkillId, out var deps))
            {
                dto.Prerequisites = deps.Select(d => new PrerequisiteSkillDto
                {
                    SkillId = d.PrerequisiteSkillId,
                    SkillName = skills.TryGetValue(d.PrerequisiteSkillId, out var ps)
                        ? ps.Name
                        : (extraSkills.TryGetValue(d.PrerequisiteSkillId, out var esName) ? esName : "Unknown")
                }).ToList();
            }

            return dto;
        }).ToList();

        return response;
    }
}
