using MediatR;
using Microsoft.Extensions.Logging;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Application.Interfaces;

namespace RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

public class GetFullUserInfoQueryHandler : IRequestHandler<GetFullUserInfoQuery, FullUserInfoResponse?>
{
    private readonly IUserProfileRepository _userProfileRepository;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IStudentEnrollmentRepository _studentEnrollmentRepository;
    private readonly IStudentSemesterSubjectRepository _studentTermSubjectRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly IUserAchievementRepository _userAchievementRepository;
    private readonly IAchievementRepository _achievementRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IMeetingParticipantRepository _meetingParticipantRepository;
    private readonly INoteRepository _noteRepository;
    private readonly INoteTagRepository _noteTagRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly ILecturerVerificationRequestRepository _lecturerVerificationRequestRepository;
    private readonly IQuestRepository _questRepository;
    private readonly IUserQuestAttemptRepository _userQuestAttemptRepository;
    private readonly IUserQuestStepProgressRepository _userQuestStepProgressRepository;
    private readonly IQuestStepRepository _questStepRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IClassRepository _classRepository;
    private readonly ICurriculumProgramRepository _curriculumProgramRepository;
    private readonly IRpcFullUserInfoService _rpcFullUserInfoService;
    private readonly ILogger<GetFullUserInfoQueryHandler> _logger;

    public GetFullUserInfoQueryHandler(
        IUserProfileRepository userProfileRepository,
        IUserRoleRepository userRoleRepository,
        IRoleRepository roleRepository,
        IStudentEnrollmentRepository studentEnrollmentRepository,
        IStudentSemesterSubjectRepository studentTermSubjectRepository,
        IUserSkillRepository userSkillRepository,
        IUserAchievementRepository userAchievementRepository,
        IAchievementRepository achievementRepository,
        IPartyMemberRepository partyMemberRepository,
        IGuildMemberRepository guildMemberRepository,
        IPartyRepository partyRepository,
        IGuildRepository guildRepository,
        IMeetingParticipantRepository meetingParticipantRepository,
        INoteRepository noteRepository,
        INoteTagRepository noteTagRepository,
        INotificationRepository notificationRepository,
        ILecturerVerificationRequestRepository lecturerVerificationRequestRepository,
        IQuestRepository questRepository,
        IUserQuestAttemptRepository userQuestAttemptRepository,
        IUserQuestStepProgressRepository userQuestStepProgressRepository,
        IQuestStepRepository questStepRepository,
        ISubjectRepository subjectRepository,
        IClassRepository classRepository,
        ICurriculumProgramRepository curriculumProgramRepository,
        IRpcFullUserInfoService rpcFullUserInfoService,
        ILogger<GetFullUserInfoQueryHandler> logger)
    {
        _userProfileRepository = userProfileRepository;
        _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
        _studentEnrollmentRepository = studentEnrollmentRepository;
        _studentTermSubjectRepository = studentTermSubjectRepository;
        _userSkillRepository = userSkillRepository;
        _userAchievementRepository = userAchievementRepository;
        _achievementRepository = achievementRepository;
        _partyMemberRepository = partyMemberRepository;
        _guildMemberRepository = guildMemberRepository;
        _partyRepository = partyRepository;
        _guildRepository = guildRepository;
        _meetingParticipantRepository = meetingParticipantRepository;
        _noteRepository = noteRepository;
        _noteTagRepository = noteTagRepository;
        _notificationRepository = notificationRepository;
        _lecturerVerificationRequestRepository = lecturerVerificationRequestRepository;
        _questRepository = questRepository;
        _userQuestAttemptRepository = userQuestAttemptRepository;
        _userQuestStepProgressRepository = userQuestStepProgressRepository;
        _questStepRepository = questStepRepository;
        _subjectRepository = subjectRepository;
        _classRepository = classRepository;
        _curriculumProgramRepository = curriculumProgramRepository;
        _rpcFullUserInfoService = rpcFullUserInfoService;
        _logger = logger;
    }

    public async Task<FullUserInfoResponse?> Handle(GetFullUserInfoQuery request, CancellationToken cancellationToken)
    {
        var profile = await _userProfileRepository.GetByAuthIdAsync(request.AuthUserId, cancellationToken);
        if (profile is null)
        {
            _logger.LogWarning("Profile not found for auth user {AuthUserId}", request.AuthUserId);
            return null;
        }

        _logger.LogInformation("Calling RPC supabase_get_full_user_info for auth user {AuthUserId}", request.AuthUserId);
        var rpcResponse = await _rpcFullUserInfoService.GetAsync(request.AuthUserId, request.PageSize, request.PageNumber, cancellationToken);
        if (rpcResponse is not null)
        {
            _logger.LogInformation("RPC supabase_get_full_user_info returned {RpcResponse}", rpcResponse);
            return rpcResponse;
        }

        _logger.LogWarning("RPC supabase_get_full_user_info returned null for auth user {AuthUserId}", request.AuthUserId);
        var response = new FullUserInfoResponse
        {
            Profile = new ProfileSection
            {
                AuthUserId = profile.AuthUserId,
                Username = profile.Username,
                Email = profile.Email,
                FirstName = profile.FirstName,
                LastName = profile.LastName,
                ClassId = profile.ClassId,
                ClassName = null,
                RouteId = profile.RouteId,
                CurriculumName = null,
                Level = profile.Level,
                ExperiencePoints = profile.ExperiencePoints,
                ProfileImageUrl = profile.ProfileImageUrl,
                OnboardingCompleted = profile.OnboardingCompleted,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            },
            Auth = new AuthSection
            {
                Id = profile.AuthUserId,
                Email = profile.Email
            }
        };

        Task<Class?> classTask = profile.ClassId.HasValue ? _classRepository.GetByIdAsync(profile.ClassId.Value, cancellationToken) : Task.FromResult<Class?>(null);
        Task<CurriculumProgram?> programTask = profile.RouteId.HasValue ? _curriculumProgramRepository.GetByIdAsync(profile.RouteId.Value, cancellationToken) : Task.FromResult<CurriculumProgram?>(null);

        var rolesTask = _userRoleRepository.GetRolesForUserAsync(request.AuthUserId, cancellationToken);
        var enrollmentsTask = _studentEnrollmentRepository.FindAsync(e => e.AuthUserId == request.AuthUserId, cancellationToken);
        var subjectsTask = _studentTermSubjectRepository.FindAsync(s => s.AuthUserId == request.AuthUserId, cancellationToken);
        var skillsTask = _userSkillRepository.FindAsync(s => s.AuthUserId == request.AuthUserId, cancellationToken);
        var userAchievementsTask = _userAchievementRepository.FindPagedAsync(ua => ua.AuthUserId == request.AuthUserId, request.PageNumber, request.PageSize, cancellationToken);
        var partyMembershipsTask = _partyMemberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        var guildMembershipsTask = _guildMemberRepository.GetMembershipsByUserAsync(request.AuthUserId, cancellationToken);
        var notesTask = _noteRepository.FindPagedAsync(n => n.AuthUserId == request.AuthUserId, request.PageNumber, request.PageSize, cancellationToken);
        var notificationsTask = _notificationRepository.GetLatestByUserAsync(request.AuthUserId, request.PageSize, cancellationToken);
        var verifsTask = _lecturerVerificationRequestRepository.FindAsync(v => v.AuthUserId == request.AuthUserId, cancellationToken);
        var attemptsTask = _userQuestAttemptRepository.FindAsync(a => a.AuthUserId == request.AuthUserId, cancellationToken);
        var meetingsTask = _meetingParticipantRepository.GetByUserAsync(request.AuthUserId, cancellationToken);

        await Task.WhenAll(
            classTask,
            programTask,
            rolesTask,
            enrollmentsTask,
            subjectsTask,
            skillsTask,
            userAchievementsTask,
            partyMembershipsTask,
            guildMembershipsTask,
            notesTask,
            notificationsTask,
            verifsTask,
            attemptsTask,
            meetingsTask
        );

        if (classTask.Result is not null)
            response.Profile.ClassName = classTask.Result!.Name;
        if (programTask.Result is not null)
            response.Profile.CurriculumName = programTask.Result!.ProgramName;

        var userRoles = rolesTask.Result;
        var roleIds = userRoles.Select(ur => ur.RoleId).Distinct().ToList();
        var roles = await _roleRepository.GetByIdsAsync(roleIds, cancellationToken);
        var roleNameMap = roles.ToDictionary(r => r.Id, r => r.Name);
        response.Relations.UserRoles = userRoles
            .Select(ur => new UserRoleItem(ur.RoleId, ur.AssignedAt, roleNameMap.GetValueOrDefault(ur.RoleId)))
            .ToList();

        var enrollments = enrollmentsTask.Result;
        response.Relations.StudentEnrollments = enrollments
            .Select(e => new StudentEnrollmentItem(e.Id, e.Status.ToString(), e.EnrollmentDate, e.ExpectedGraduationDate))
            .ToList();

        var subjects = subjectsTask.Result;
        var subjectIds = subjects.Select(s => s.SubjectId).Distinct().ToList();
        var subjectModels = await _subjectRepository.GetByIdsAsync(subjectIds, cancellationToken);
        var subjMap = subjectModels.ToDictionary(sm => sm.Id, sm => sm);
        response.Relations.StudentTermSubjects = subjects
            .Select(s =>
            {
                var m = subjMap.GetValueOrDefault(s.SubjectId);
                return new StudentTermSubjectItem(
                    s.Id,
                    s.SubjectId,
                    m?.SubjectCode ?? string.Empty,
                    m?.SubjectName ?? string.Empty,
                    m?.Semester,
                    s.Status.ToString(),
                    s.Grade
                );
            })
            .OrderBy(st => st.Semester ?? int.MaxValue)
            .ToList();

        var skills = skillsTask.Result;
        response.Relations.UserSkills = skills
            .Select(s => new UserSkillItem(s.Id, s.SkillName, s.Level, s.ExperiencePoints))
            .ToList();

        var userAchievements = userAchievementsTask.Result;
        var achIds = userAchievements.Select(ua => ua.AchievementId).Distinct().ToList();
        var achs = await _achievementRepository.GetByIdsAsync(achIds, cancellationToken);
        var achNameMap = achs.ToDictionary(a => a.Id, a => a.Name);
        response.Relations.UserAchievements = userAchievements
            .Select(ua => new UserAchievementItem(ua.AchievementId, ua.EarnedAt, achNameMap.GetValueOrDefault(ua.AchievementId)))
            .ToList();

        var partyMemberships = partyMembershipsTask.Result;
        var partyIds = partyMemberships.Select(pm => pm.PartyId).Distinct().ToList();
        var parties = await _partyRepository.GetByIdsAsync(partyIds, cancellationToken);
        var partyNameMap = parties.ToDictionary(p => p.Id, p => p.Name);
        response.Relations.PartyMembers = partyMemberships
            .Select(pm => new PartyMemberItem(pm.PartyId, partyNameMap.GetValueOrDefault(pm.PartyId) ?? string.Empty, pm.Role.ToString(), pm.JoinedAt))
            .ToList();

        var guildMemberships = guildMembershipsTask.Result;
        var guildIds = guildMemberships.Select(gm => gm.GuildId).Distinct().ToList();
        var guilds = await _guildRepository.GetByIdsAsync(guildIds, cancellationToken);
        var guildNameMap = guilds.ToDictionary(g => g.Id, g => g.Name);
        response.Relations.GuildMembers = guildMemberships
            .Select(gm => new GuildMemberItem(gm.GuildId, guildNameMap.GetValueOrDefault(gm.GuildId) ?? string.Empty, gm.Role.ToString(), gm.JoinedAt))
            .ToList();

        var meetingParticipants = await _meetingParticipantRepository.GetByUserAsync(request.AuthUserId, cancellationToken);

        var notes = notesTask.Result;
        response.Relations.Notes = notes.Select(n => new NoteItem(n.Id, n.Title, n.CreatedAt)).ToList();

        var notifications = notificationsTask.Result;
        response.Relations.Notifications = notifications
            .Select(n => new NotificationItem(n.Id, n.Type.ToString(), n.Title, n.IsRead, n.CreatedAt))
            .ToList();

        var verifs = verifsTask.Result;
        response.Relations.LecturerVerificationRequests = verifs
            .Select(v => new LecturerVerificationRequestItem(v.Id, v.Status.ToString(), v.SubmittedAt))
            .ToList();

        var attempts = attemptsTask.Result;
        var questIds = attempts.Select(a => a.QuestId).Distinct().ToList();
        var quests = await _questRepository.GetByIdsAsync(questIds, cancellationToken);
        var questTitleMap = quests.ToDictionary(q => q.Id, q => q.Title);

        foreach (var attempt in attempts)
        {
            var steps = await _questStepRepository.GetByQuestIdAsync(attempt.QuestId, cancellationToken);
            var stepsTotal = steps.Count();
            var stepsCompleted = await _userQuestStepProgressRepository.GetCompletedStepsCountForAttemptAsync(attempt.Id, cancellationToken);

            response.Relations.QuestAttempts.Add(new QuestAttemptItem(
                attempt.Id,
                attempt.QuestId,
                questTitleMap.GetValueOrDefault(attempt.QuestId) ?? string.Empty,
                attempt.Status.ToString(),
                attempt.CompletionPercentage,
                attempt.TotalExperienceEarned,
                attempt.StartedAt,
                attempt.CompletedAt,
                stepsTotal,
                stepsCompleted,
                attempt.CurrentStepId
            ));
        }

        response.Counts.Notes = await _noteRepository.CountAsync(n => n.AuthUserId == request.AuthUserId, cancellationToken);
        response.Counts.Achievements = await _userAchievementRepository.CountAsync(ua => ua.AuthUserId == request.AuthUserId, cancellationToken);
        response.Counts.Meetings = meetingsTask.Result.Count();
        response.Counts.NotificationsUnread = await _notificationRepository.CountUnreadByUserAsync(request.AuthUserId, cancellationToken);
        var attemptsForCounts = attempts;
        response.Counts.QuestsCompleted = attemptsForCounts.Count(a => a.Status == QuestAttemptStatus.Completed);
        response.Counts.QuestsInProgress = attemptsForCounts.Count(a => a.Status == QuestAttemptStatus.InProgress);

        return response;
    }

    
}