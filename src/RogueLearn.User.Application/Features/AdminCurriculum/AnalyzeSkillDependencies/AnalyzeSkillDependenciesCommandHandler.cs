// RogueLearn.User/src/RogueLearn.User.Application/Features/AdminCurriculum/AnalyzeSkillDependencies/AnalyzeSkillDependenciesCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.AdminCurriculum.AnalyzeSkillDependencies;

public class AnalyzeSkillDependenciesCommandHandler : IRequestHandler<AnalyzeSkillDependenciesCommand, AnalyzeSkillDependenciesResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillDependencyAnalysisPlugin _skillDependencyPlugin;
    private readonly ILogger<AnalyzeSkillDependenciesCommandHandler> _logger;

    public AnalyzeSkillDependenciesCommandHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISkillRepository skillRepository,
        ISkillDependencyAnalysisPlugin skillDependencyPlugin,
        ILogger<AnalyzeSkillDependenciesCommandHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _skillRepository = skillRepository;
        _skillDependencyPlugin = skillDependencyPlugin;
        _logger = logger;
    }

    public async Task<AnalyzeSkillDependenciesResponse> Handle(AnalyzeSkillDependenciesCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Admin Tool: Analyzing skill dependencies for CurriculumVersion {CurriculumVersionId}", request.CurriculumVersionId);

        var response = new AnalyzeSkillDependenciesResponse();

        var curriculumStructures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId, cancellationToken)).ToList();

        var subjectIdsInCurriculum = curriculumStructures.Select(cs => cs.SubjectId).ToHashSet();

        var allCatalogSkills = await _skillRepository.GetAllAsync(cancellationToken);
        var allSkillsInCurriculum = allCatalogSkills
            .Where(s => s.SourceSubjectId.HasValue && subjectIdsInCurriculum.Contains(s.SourceSubjectId.Value))
            .ToList();

        if (!allSkillsInCurriculum.Any())
        {
            throw new NotFoundException("No skills are sourced from any subjects in this curriculum version.");
        }

        var skillsBySubject = allSkillsInCurriculum
            .GroupBy(s => s.SourceSubjectId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // --- Stage 1: Structural Analysis based on curriculum prerequisites ---
        foreach (var structure in curriculumStructures)
        {
            if (!skillsBySubject.TryGetValue(structure.SubjectId, out var currentSkills))
                continue;

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

            foreach (var currentSkill in currentSkills)
            {
                foreach (var prereqSkill in prerequisiteSkills)
                {
                    response.SuggestedDependencies.Add(new SuggestedDependencyDto
                    {
                        SkillName = currentSkill.Name,
                        PrerequisiteSkillName = prereqSkill.Name,
                        RelationshipType = "Prerequisite",
                        Reasoning = $"Derived from curriculum structure: {prereqSkill.Name}'s subject is a prerequisite for {currentSkill.Name}'s subject."
                    });
                }
            }
        }
        _logger.LogInformation("Structural analysis found {Count} potential dependencies.", response.SuggestedDependencies.Count);

        // --- Stage 2: AI Semantic Analysis within each subject ---
        foreach (var subjectSkillGroup in skillsBySubject.Values)
        {
            if (subjectSkillGroup.Count <= 1) continue;

            var skillNames = subjectSkillGroup.Select(s => s.Name).ToList();
            var aiDependencies = await _skillDependencyPlugin.AnalyzeSkillDependenciesAsync(skillNames, cancellationToken);

            foreach (var aiDep in aiDependencies)
            {
                response.SuggestedDependencies.Add(new SuggestedDependencyDto
                {
                    SkillName = aiDep.SkillName,
                    PrerequisiteSkillName = aiDep.PrerequisiteSkillName,
                    RelationshipType = aiDep.RelationshipType,
                    Reasoning = aiDep.Reasoning
                });
            }
        }
        _logger.LogInformation("Completed AI analysis. Total suggested dependencies: {Count}", response.SuggestedDependencies.Count);

        response.Message = $"Analysis complete. Found {response.SuggestedDependencies.Count} potential skill dependencies for review.";
        return response;
    }
}
