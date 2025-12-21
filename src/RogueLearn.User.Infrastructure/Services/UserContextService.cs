using Microsoft.Extensions.Logging;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Interfaces;
using System.Text.Json;

namespace RogueLearn.User.Infrastructure.Services;

public class UserContextService : IUserContextService
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IClassRepository _classRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly ILogger<UserContextService> _logger;

    public UserContextService(
        IUserProfileRepository userProfileRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IClassRepository classRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository,
        IUserSkillRepository userSkillRepository,
        IUserAchievementRepository userAchievementRepository,
        ILogger<UserContextService> logger)
    {
        _userProfileRepository = userProfileRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _classRepository = classRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
        _userSkillRepository = userSkillRepository;
        _userAchievementRepository = userAchievementRepository;
        _logger = logger;
    }

    public async Task<UserContextDto?> BuildForAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var profile = await _userProfileRepository.GetByAuthIdAsync(authUserId, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning("No user profile found for auth user id {AuthUserId}", authUserId);
            return null;
        }

        var displayName = (!string.IsNullOrWhiteSpace(profile.FirstName) || !string.IsNullOrWhiteSpace(profile.LastName))
            ? ($"{profile.FirstName} {profile.LastName}").Trim()
            : profile.Username;

        var dto = new UserContextDto
        {
            AuthUserId = profile.AuthUserId,
            Username = profile.Username,
            Email = profile.Email,
            DisplayName = displayName,
            ProfileImageUrl = profile.ProfileImageUrl,
            Bio = profile.Bio,
        };

        var userRoles = await _userRoleRepository.GetRolesForUserAsync(authUserId, cancellationToken);
        if (userRoles.Any())
        {
            var roleTasks = userRoles.Select(ur => _roleRepository.GetByIdAsync(ur.RoleId, cancellationToken));
            var roleResults = await Task.WhenAll(roleTasks);
            dto.Roles = roleResults.Where(r => r != null).Select(r => r!.Name).Distinct().ToList();
        }

        if (profile.ClassId.HasValue)
        {
            var userClass = await _classRepository.GetByIdAsync(profile.ClassId.Value, cancellationToken);
            if (userClass is not null)
            {
                dto.Class = new ClassSummaryDto
                {
                    Id = userClass.Id,
                    Name = userClass.Name,
                    RoadmapUrl = userClass.RoadmapUrl,
                    DifficultyLevel = (int)userClass.DifficultyLevel,
                    SkillFocusAreas = userClass.SkillFocusAreas
                };
            }
        }

        CurriculumEnrollmentDto? enrollmentDto = null;
        // Fetch active enrollment using explicit string filters to avoid enum numeric mismatch (22P02)
        var activeEnrollment = await _studentEnrollmentRepository.GetActiveForAuthUserAsync(authUserId, cancellationToken);
        var enrollment = activeEnrollment;
        if (enrollment is null)
        {
            // fallback to any enrollment
            enrollment = await _studentEnrollmentRepository.FirstOrDefaultAsync(e => e.AuthUserId == authUserId, cancellationToken);
        }
        dto.Enrollment = enrollmentDto;

        var skills = await _userSkillRepository.FindAsync(x => x.AuthUserId == authUserId, cancellationToken);
        var skillsList = skills.ToList();
        var totalExperience = skillsList.Sum(s => s.ExperiencePoints);
        var highestLevel = skillsList.Any() ? skillsList.Max(s => s.Level) : 0;
        var averageLevel = skillsList.Any() ? skillsList.Average(s => s.Level) : 0;
        var topSkills = skillsList
            .OrderByDescending(s => s.ExperiencePoints)
            .ThenByDescending(s => s.Level)
            .Take(5)
            .Select(s => new UserSkillDto
            {
                SkillName = s.SkillName,
                Level = s.Level,
                ExperiencePoints = s.ExperiencePoints,
                LastUpdatedAt = s.LastUpdatedAt
            })
            .ToList();

        dto.Skills = new SkillSummaryDto
        {
            TotalSkills = skillsList.Count,
            TotalExperiencePoints = totalExperience,
            HighestLevel = highestLevel,
            AverageLevel = Math.Round(averageLevel, 2),
            TopSkills = topSkills
        };

        dto.AchievementsCount = await _userAchievementRepository.CountAsync(x => x.AuthUserId == authUserId, cancellationToken);

        return dto;
    }
}
