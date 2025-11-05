// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/InitializeUserSkills/InitializeUserSkillsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Application.Features.Student.Commands.InitializeUserSkills;

public class InitializeUserSkillsCommandHandler : IRequestHandler<InitializeUserSkillsCommand, InitializeUserSkillsResponse>
{
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISyllabusVersionRepository _syllabusVersionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISubjectExtractionPlugin _subjectExtractionPlugin;
    private readonly ILogger<InitializeUserSkillsCommandHandler> _logger;

    public InitializeUserSkillsCommandHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISyllabusVersionRepository syllabusVersionRepository,
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ISubjectExtractionPlugin subjectExtractionPlugin,
        ILogger<InitializeUserSkillsCommandHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _syllabusVersionRepository = syllabusVersionRepository;
        _skillRepository = skillRepository;
        _userSkillRepository = userSkillRepository;
        _subjectExtractionPlugin = subjectExtractionPlugin;
        _logger = logger;
    }

    public async Task<InitializeUserSkillsResponse> Handle(InitializeUserSkillsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing skill tree for User {AuthUserId} and CurriculumVersion {CurriculumVersionId}",
            request.AuthUserId, request.CurriculumVersionId);

        var response = new InitializeUserSkillsResponse();

        var structures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken)).ToList();

        var subjectIds = structures.Select(s => s.SubjectId).Distinct().ToList();

        var allSyllabiResults = await _syllabusVersionRepository.GetActiveBySubjectIdsAsync(subjectIds, cancellationToken);
        var allSyllabi = allSyllabiResults
            .GroupBy(sv => sv.SubjectId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(sv => sv.VersionNumber).FirstOrDefault());

        // MODIFICATION: Instead of one big list, we group objectives by subjectId.
        var objectivesBySubject = new Dictionary<Guid, List<string>>();
        foreach (var subjectId in subjectIds)
        {
            if (allSyllabi.TryGetValue(subjectId, out var activeSyllabus) && activeSyllabus?.Content != null)
            {
                var subjectObjectives = new List<string>();
                try
                {
                    if (activeSyllabus.Content.TryGetValue("sessions", out var sessionsObj) &&
                        sessionsObj is Newtonsoft.Json.Linq.JArray sessionsArray)
                    {
                        foreach (var session in sessionsArray)
                        {
                            var sessionObject = session as Newtonsoft.Json.Linq.JObject;
                            if (sessionObject != null &&
                                sessionObject.TryGetValue("lo", StringComparison.OrdinalIgnoreCase, out var loToken) &&
                                loToken.Type == Newtonsoft.Json.Linq.JTokenType.String)
                            {
                                var learningObjective = loToken.ToString();
                                if (!string.IsNullOrWhiteSpace(learningObjective))
                                {
                                    subjectObjectives.Add(learningObjective);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not process syllabus content for SubjectId {SubjectId}", subjectId);
                }
                if (subjectObjectives.Any())
                {
                    objectivesBySubject[subjectId] = subjectObjectives;
                }
            }
        }

        if (!objectivesBySubject.Any())
        {
            _logger.LogWarning("No learning objectives found for curriculum {CurriculumVersionId}", request.CurriculumVersionId);
            response.IsSuccess = false;
            response.Message = "No learning objectives found in curriculum syllabi.";
            return response;
        }

        var allCatalogSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var existingUserSkills = (await _userSkillRepository.FindAsync(
            us => us.AuthUserId == request.AuthUserId,
            cancellationToken))
            .ToDictionary(us => us.SkillName, StringComparer.OrdinalIgnoreCase);

        int skillsInitialized = 0;
        int skillsSkipped = 0;

        // MODIFICATION: We now iterate through each subject to process its skills.
        foreach (var entry in objectivesBySubject)
        {
            var subjectId = entry.Key;
            var objectives = entry.Value;

            if (!objectives.Any()) continue;

            var extractedSkills = await _subjectExtractionPlugin.ExtractSkillsFromObjectivesAsync(objectives, cancellationToken);
            var uniqueSkillNames = extractedSkills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            response.TotalSkillsExtracted += uniqueSkillNames.Count;

            foreach (var skillName in uniqueSkillNames)
            {
                Skill? catalogSkill;
                if (!allCatalogSkills.TryGetValue(skillName, out catalogSkill))
                {
                    _logger.LogInformation("Skill '{SkillName}' not found in catalog. Creating new skill entry from Subject {SubjectId}.", skillName, subjectId);

                    var newSkill = new Skill
                    {
                        Name = skillName,
                        Domain = "Academic",
                        Tier = Domain.Enums.SkillTierLevel.Foundation,
                        Description = $"Auto-generated skill from curriculum analysis.",
                        // MODIFICATION: Populate the new source_subject_id column.
                        SourceSubjectId = subjectId,
                        CreatedAt = DateTimeOffset.UtcNow,
                        UpdatedAt = DateTimeOffset.UtcNow
                    };

                    try
                    {
                        catalogSkill = await _skillRepository.AddAsync(newSkill, cancellationToken);
                        allCatalogSkills[skillName] = catalogSkill;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to create skill '{SkillName}' in catalog. Skipping.", skillName);
                        continue;
                    }
                }

                if (!existingUserSkills.ContainsKey(skillName))
                {
                    var newUserSkill = new UserSkill
                    {
                        AuthUserId = request.AuthUserId,
                        SkillId = catalogSkill.Id,
                        SkillName = catalogSkill.Name,
                        ExperiencePoints = 0,
                        Level = 1,
                        LastUpdatedAt = DateTimeOffset.UtcNow
                    };
                    await _userSkillRepository.AddAsync(newUserSkill, cancellationToken);
                    skillsInitialized++;
                    existingUserSkills[skillName] = newUserSkill;
                }
                else
                {
                    skillsSkipped++;
                }
            }
        }

        response.IsSuccess = true;
        response.Message = $"Skill initialization complete. {skillsInitialized} skills initialized, {skillsSkipped} already existed.";
        response.SkillsInitialized = skillsInitialized;
        response.SkillsSkipped = skillsSkipped;

        _logger.LogInformation(
            "Skill initialization completed for User {AuthUserId}. Extracted: {Extracted}, Initialized: {Initialized}, Skipped: {Skipped}",
            request.AuthUserId, response.TotalSkillsExtracted, skillsInitialized, skillsSkipped);

        return response;
    }
}