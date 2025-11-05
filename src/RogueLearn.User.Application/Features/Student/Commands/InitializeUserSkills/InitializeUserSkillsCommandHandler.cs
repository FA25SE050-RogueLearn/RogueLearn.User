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

        // Get curriculum structures
        var structures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken)).ToList();

        _logger.LogDebug("Found {Count} structures for curriculum version.", structures.Count);

        var subjectIds = structures.Select(s => s.SubjectId).Distinct().ToList();
        _logger.LogDebug("Found {Count} unique subjects in curriculum.", subjectIds.Count);

        // Get active syllabi
        var allSyllabiResults = await _syllabusVersionRepository.GetActiveBySubjectIdsAsync(subjectIds, cancellationToken);
        var allSyllabi = allSyllabiResults
            .GroupBy(sv => sv.SubjectId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(sv => sv.VersionNumber).FirstOrDefault());

        _logger.LogDebug("Fetched {Count} active syllabi for the subjects in the curriculum.", allSyllabi.Count);

        // Extract all learning objectives
        var allLearningObjectives = new List<string>();
        foreach (var subjectId in subjectIds)
        {
            if (allSyllabi.TryGetValue(subjectId, out var activeSyllabus) && activeSyllabus?.Content != null)
            {
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
                                    allLearningObjectives.Add(learningObjective);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not process syllabus content for SubjectId {SubjectId}", subjectId);
                }
            }
        }

        if (!allLearningObjectives.Any())
        {
            _logger.LogWarning("No learning objectives found for curriculum {CurriculumVersionId}", request.CurriculumVersionId);
            response.IsSuccess = false;
            response.Message = "No learning objectives found in curriculum syllabi.";
            return response;
        }

        // Call AI for batch skill extraction
        _logger.LogInformation("Sending {Count} learning objectives to AI for batch skill extraction.", allLearningObjectives.Count);
        var extractedSkills = await _subjectExtractionPlugin.ExtractSkillsFromObjectivesAsync(allLearningObjectives, cancellationToken);
        _logger.LogInformation("AI returned {Count} skill extractions.", extractedSkills.Count);

        // Get unique, non-empty skill names (case-insensitive)
        var uniqueSkillNames = extractedSkills
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        response.TotalSkillsExtracted = uniqueSkillNames.Count;

        if (!uniqueSkillNames.Any())
        {
            _logger.LogWarning("No skills extracted from learning objectives.");
            response.IsSuccess = false;
            response.Message = "AI failed to extract any skills from learning objectives.";
            return response;
        }

        // Get existing user skills
        var existingUserSkills = (await _userSkillRepository.FindAsync(
            us => us.AuthUserId == request.AuthUserId,
            cancellationToken))
            .ToDictionary(us => us.SkillName, StringComparer.OrdinalIgnoreCase);

        // Get all catalog skills
        var allCatalogSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        // CRITICAL CHANGE: Auto-create missing skills in catalog
        var missingSkills = new List<string>();
        var skillsToInitialize = new List<Skill>();

        foreach (var skillName in uniqueSkillNames)
        {
            if (!allCatalogSkills.ContainsKey(skillName))
            {
                _logger.LogInformation("Skill '{SkillName}' not found in catalog. Creating new skill entry.", skillName);

                var newSkill = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Domain = "Academic", // Default domain
                    Tier = Domain.Enums.SkillTierLevel.Foundation, // Default tier
                    Description = $"Auto-generated skill from curriculum analysis: {skillName}",
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                try
                {
                    var createdSkill = await _skillRepository.AddAsync(newSkill, cancellationToken);
                    allCatalogSkills[skillName] = createdSkill;
                    _logger.LogInformation("Successfully created skill '{SkillName}' with ID {SkillId}", skillName, createdSkill.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to create skill '{SkillName}' in catalog. Skipping.", skillName);
                    missingSkills.Add(skillName);
                    continue;
                }
            }
        }

        // Initialize user skills
        int skillsInitialized = 0;
        int skillsSkipped = 0;

        foreach (var skillName in uniqueSkillNames)
        {
            // Skip if user already has this skill
            if (existingUserSkills.ContainsKey(skillName))
            {
                skillsSkipped++;
                _logger.LogDebug("User already has skill '{SkillName}'. Skipping.", skillName);
                continue;
            }

            // Get from catalog (should exist now after auto-creation)
            if (allCatalogSkills.TryGetValue(skillName, out var catalogSkill))
            {
                var newUserSkill = new UserSkill
                {
                    Id = Guid.NewGuid(),
                    AuthUserId = request.AuthUserId,
                    SkillId = catalogSkill.Id,
                    SkillName = catalogSkill.Name,
                    ExperiencePoints = 0,
                    Level = 1,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                };

                try
                {
                    await _userSkillRepository.AddAsync(newUserSkill, cancellationToken);
                    skillsInitialized++;
                    _logger.LogInformation("Initialized UserSkill '{SkillName}' for User {AuthUserId}", skillName, request.AuthUserId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize UserSkill '{SkillName}' for User {AuthUserId}", skillName, request.AuthUserId);
                }
            }
            else
            {
                missingSkills.Add(skillName);
                _logger.LogWarning("Skill '{SkillName}' still not in catalog after auto-creation attempt.", skillName);
            }
        }

        response.IsSuccess = true;
        response.Message = $"Skill initialization complete. {skillsInitialized} skills initialized, {skillsSkipped} already existed.";
        response.SkillsInitialized = skillsInitialized;
        response.SkillsSkipped = skillsSkipped;
        response.MissingFromCatalog = missingSkills;

        _logger.LogInformation(
            "Skill initialization completed for User {AuthUserId}. Extracted: {Extracted}, Initialized: {Initialized}, Skipped: {Skipped}, Missing: {Missing}",
            request.AuthUserId, response.TotalSkillsExtracted, skillsInitialized, skillsSkipped, missingSkills.Count);

        return response;
    }
}