// RogueLearn.User/src/RogueLearn.User.Application/Features/Student/Commands/InitializeUserSkills/InitializeUserSkillsCommandHandler.cs
using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Student.Commands.InitializeUserSkills;

public class InitializeUserSkillsCommandHandler : IRequestHandler<InitializeUserSkillsCommand, InitializeUserSkillsResponse>
{
    // MODIFIED: Removed dependencies on AI plugins and syllabus repository.
    private readonly ICurriculumStructureRepository _curriculumStructureRepository;
    private readonly ISubjectSkillMappingRepository _subjectSkillMappingRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ILogger<InitializeUserSkillsCommandHandler> _logger;

    public InitializeUserSkillsCommandHandler(
        ICurriculumStructureRepository curriculumStructureRepository,
        ISubjectSkillMappingRepository subjectSkillMappingRepository,
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<InitializeUserSkillsCommandHandler> logger)
    {
        _curriculumStructureRepository = curriculumStructureRepository;
        _subjectSkillMappingRepository = subjectSkillMappingRepository;
        _skillRepository = skillRepository;
        _userSkillRepository = userSkillRepository;
        _logger = logger;
    }

    public async Task<InitializeUserSkillsResponse> Handle(InitializeUserSkillsCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing user skills for User {AuthUserId} and CurriculumVersion {CurriculumVersionId}",
            request.AuthUserId, request.CurriculumVersionId);

        var response = new InitializeUserSkillsResponse();

        // 1. Find all subjects for the given curriculum version.
        var structures = (await _curriculumStructureRepository.FindAsync(
            cs => cs.CurriculumVersionId == request.CurriculumVersionId,
            cancellationToken)).ToList();

        if (!structures.Any())
        {
            _logger.LogWarning("No curriculum structure found for CurriculumVersion {CurriculumVersionId}", request.CurriculumVersionId);
            response.IsSuccess = false;
            response.Message = "No curriculum structure found for this version.";
            return response;
        }

        var subjectIds = structures.Select(s => s.SubjectId).Distinct().ToList();

        // 2. Use the new mapping table to find all skills linked to those subjects.
        var mappings = await _subjectSkillMappingRepository.GetMappingsBySubjectIdsAsync(subjectIds, cancellationToken);
        var skillIdsToInitialize = mappings.Select(m => m.SkillId).Distinct().ToList();

        if (!skillIdsToInitialize.Any())
        {
            _logger.LogWarning("No skills are mapped to the subjects in CurriculumVersion {CurriculumVersionId}", request.CurriculumVersionId);
            response.IsSuccess = false;
            response.Message = "No skills are mapped to this curriculum.";
            return response;
        }

        // 3. Get the full skill details from the master skills table.
        var allCatalogSkills = (await _skillRepository.GetAllAsync(cancellationToken))
            .Where(s => skillIdsToInitialize.Contains(s.Id))
            .ToDictionary(s => s.Id);

        // 4. Get the user's currently tracked skills to avoid duplicates.
        var existingUserSkills = (await _userSkillRepository.GetSkillsByAuthIdAsync(request.AuthUserId, cancellationToken))
            .ToDictionary(us => us.SkillId);

        int skillsInitialized = 0;
        int skillsSkipped = 0;
        var skillsToCreate = new List<UserSkill>();

        // 5. For each skill mapped to the curriculum, create a `user_skills` record if it doesn't exist.
        foreach (var skillId in skillIdsToInitialize)
        {
            if (!allCatalogSkills.TryGetValue(skillId, out var catalogSkill))
            {
                _logger.LogWarning("Skill mapping points to a non-existent SkillId {SkillId}. Skipping.", skillId);
                continue;
            }

            if (!existingUserSkills.ContainsKey(skillId))
            {
                skillsToCreate.Add(new UserSkill
                {
                    AuthUserId = request.AuthUserId,
                    SkillId = catalogSkill.Id,
                    SkillName = catalogSkill.Name, // This is now for display/denormalization, not the primary link.
                    ExperiencePoints = 0,
                    Level = 1,
                    LastUpdatedAt = DateTimeOffset.UtcNow
                });
                skillsInitialized++;
            }
            else
            {
                skillsSkipped++;
            }
        }

        if (skillsToCreate.Any())
        {
            await _userSkillRepository.AddRangeAsync(skillsToCreate, cancellationToken);
            _logger.LogInformation("Batch created {Count} new user_skills records for user {AuthUserId}", skillsToCreate.Count, request.AuthUserId);
        }

        response.IsSuccess = true;
        response.Message = $"Skill initialization complete. {skillsInitialized} skills initialized, {skillsSkipped} already existed.";
        response.SkillsInitialized = skillsInitialized;
        response.SkillsSkipped = skillsSkipped;

        _logger.LogInformation(
            "Skill initialization completed for User {AuthUserId}. Initialized: {Initialized}, Skipped: {Skipped}",
            request.AuthUserId, skillsInitialized, skillsSkipped);

        return response;
    }
}