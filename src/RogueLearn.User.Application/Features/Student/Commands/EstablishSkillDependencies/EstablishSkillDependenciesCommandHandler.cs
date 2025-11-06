// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/EstablishSkillDependencies/EstablishSkillDependenciesCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Student.Commands.EstablishSkillDependencies;

public class EstablishSkillDependenciesCommandHandler : IRequestHandler<EstablishSkillDependenciesCommand, EstablishSkillDependenciesResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillDependencyRepository _skillDependencyRepository;
    private readonly ISkillDependencyAnalysisPlugin _skillDependencyPlugin;
    private readonly ILogger<EstablishSkillDependenciesCommandHandler> _logger;

    public EstablishSkillDependenciesCommandHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISkillRepository skillRepository,
        ISkillDependencyRepository skillDependencyRepository,
        ISkillDependencyAnalysisPlugin skillDependencyPlugin,
        ILogger<EstablishSkillDependenciesCommandHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _skillRepository = skillRepository;
        _skillDependencyRepository = skillDependencyRepository;
        _skillDependencyPlugin = skillDependencyPlugin;
        _logger = logger;
    }

    public async Task<EstablishSkillDependenciesResponse> Handle(EstablishSkillDependenciesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Establishing skill dependencies for CurriculumVersion {CurriculumVersionId}", request.CurriculumVersionId);

        var response = new EstablishSkillDependenciesResponse();

        var curriculumStructures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId, cancellationToken)).ToList();

        var subjectIdsInCurriculum = curriculumStructures.Select(cs => cs.SubjectId).ToHashSet();

        // --- MODIFICATION START ---
        // The Supabase LINQ provider cannot translate a .Contains() call on a local collection (like subjectIdsInCurriculum).
        // To fix this, we fetch all skills first, and then filter them in-memory.
        // This is less efficient for very large skill catalogs but is robust and works with the current provider.
        var allCatalogSkills = await _skillRepository.GetAllAsync(cancellationToken);
        var allSkillsInCurriculum = allCatalogSkills
            .Where(s => s.SourceSubjectId.HasValue && subjectIdsInCurriculum.Contains(s.SourceSubjectId.Value))
            .ToList();
        // --- MODIFICATION END ---

        if (!allSkillsInCurriculum.Any())
        {
            response.IsSuccess = false;
            response.Message = "No skills found for this curriculum. Please initialize skills first.";
            return response;
        }

        // Group skills by their source subject for easy lookup
        var skillsBySubject = allSkillsInCurriculum
            .GroupBy(s => s.SourceSubjectId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get existing dependencies to avoid duplicates
        var existingDependencies = (await _skillDependencyRepository.GetAllAsync(cancellationToken))
            .Select(sd => $"{sd.SkillId}_{sd.PrerequisiteSkillId}")
            .ToHashSet();

        int dependenciesCreated = 0;
        int dependenciesSkipped = 0;

        // --- Stage 1: Create dependencies based on curriculum structure (semesters) ---
        foreach (var structure in curriculumStructures)
        {
            if (!skillsBySubject.TryGetValue(structure.SubjectId, out var currentSkills))
                continue;

            // Find skills from all prerequisite subjects defined in the curriculum
            var prerequisiteSkills = new List<Skill>();
            if (structure.PrerequisiteSubjectIds != null)
            {
                foreach (var prereqSubjectId in structure.PrerequisiteSubjectIds)
                {
                    if (skillsBySubject.TryGetValue(prereqSubjectId, out var prereqSubjectSkills))
                    {
                        prerequisiteSkills.AddRange(prereqSubjectSkills);
                    }
                }
            }

            if (!prerequisiteSkills.Any()) continue;

            // Create a dependency from every current skill to every prerequisite skill
            foreach (var currentSkill in currentSkills)
            {
                foreach (var prereqSkill in prerequisiteSkills)
                {
                    var dependencyKey = $"{currentSkill.Id}_{prereqSkill.Id}";
                    if (!existingDependencies.Contains(dependencyKey))
                    {
                        var dependency = new SkillDependency
                        {
                            SkillId = currentSkill.Id,
                            PrerequisiteSkillId = prereqSkill.Id,
                            RelationshipType = SkillRelationshipType.Prerequisite,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _skillDependencyRepository.AddAsync(dependency, cancellationToken);
                        response.Dependencies.Add(new SkillDependencyInfo { SkillName = currentSkill.Name, PrerequisiteSkillName = prereqSkill.Name, RelationshipType = "Prerequisite" });
                        existingDependencies.Add(dependencyKey);
                        dependenciesCreated++;
                    }
                    else
                    {
                        dependenciesSkipped++;
                    }
                }
            }
        }

        _logger.LogInformation("Structural analysis complete. Created {Count} dependencies based on curriculum prerequisites.", dependenciesCreated);

        // --- Stage 2: Use AI to analyze semantic relationships within each subject ---
        foreach (var subjectSkillGroup in skillsBySubject.Values)
        {
            if (subjectSkillGroup.Count <= 1) continue;

            var skillNames = subjectSkillGroup.Select(s => s.Name).ToList();
            var aiDependencies = await _skillDependencyPlugin.AnalyzeSkillDependenciesAsync(skillNames, cancellationToken);

            foreach (var aiDep in aiDependencies)
            {
                var skill = subjectSkillGroup.FirstOrDefault(s => s.Name.Equals(aiDep.SkillName, StringComparison.OrdinalIgnoreCase));
                var prereqSkill = subjectSkillGroup.FirstOrDefault(s => s.Name.Equals(aiDep.PrerequisiteSkillName, StringComparison.OrdinalIgnoreCase));

                if (skill != null && prereqSkill != null && skill.Id != prereqSkill.Id)
                {
                    var dependencyKey = $"{skill.Id}_{prereqSkill.Id}";
                    if (!existingDependencies.Contains(dependencyKey))
                    {
                        var relationshipType = aiDep.RelationshipType switch
                        {
                            "Complements" => SkillRelationshipType.Recommended,
                            "Alternative" => SkillRelationshipType.Corequisite,
                            _ => SkillRelationshipType.Prerequisite
                        };

                        var dependency = new SkillDependency
                        {
                            SkillId = skill.Id,
                            PrerequisiteSkillId = prereqSkill.Id,
                            RelationshipType = relationshipType,
                            CreatedAt = DateTimeOffset.UtcNow
                        };
                        await _skillDependencyRepository.AddAsync(dependency, cancellationToken);
                        response.Dependencies.Add(new SkillDependencyInfo { SkillName = skill.Name, PrerequisiteSkillName = prereqSkill.Name, RelationshipType = relationshipType.ToString() });
                        existingDependencies.Add(dependencyKey);
                        dependenciesCreated++;
                    }
                    else
                    {
                        dependenciesSkipped++;
                    }
                }
            }
        }

        _logger.LogInformation("AI analysis complete. Total dependencies created: {Created}. Total skipped: {Skipped}", dependenciesCreated, dependenciesSkipped);

        response.IsSuccess = true;
        response.Message = $"Skill dependencies established. {dependenciesCreated} created, {dependenciesSkipped} skipped.";
        response.TotalDependenciesCreated = dependenciesCreated;
        response.TotalDependenciesSkipped = dependenciesSkipped;

        return response;
    }
}