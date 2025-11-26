using MediatR;

namespace RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;

public class GetFullUserInfoQuery : IRequest<FullUserInfoResponse?>
{
    public Guid AuthUserId { get; set; }
    public int PageSize { get; set; } = 20;
    public int PageNumber { get; set; } = 1;
}

public class FullUserInfoResponse
{
    public ProfileSection Profile { get; set; } = new();
    public AuthSection Auth { get; set; } = new();
    public RelationsSection Relations { get; set; } = new();
    public CountsSection Counts { get; set; } = new();
}

public class ProfileSection
{
    public Guid AuthUserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public Guid? ClassId { get; set; }
    public Guid? RouteId { get; set; }
    public int Level { get; set; }
    public int ExperiencePoints { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool OnboardingCompleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class AuthSection
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool? EmailVerified { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? LastSignInAt { get; set; }
    public Dictionary<string, object>? UserMetadata { get; set; }
}

public class RelationsSection
{
    public List<UserRoleItem> UserRoles { get; set; } = new();
    public List<StudentEnrollmentItem> StudentEnrollments { get; set; } = new();
    public List<StudentTermSubjectItem> StudentTermSubjects { get; set; } = new();
    public List<UserSkillItem> UserSkills { get; set; } = new();
    public List<UserAchievementItem> UserAchievements { get; set; } = new();
    public List<PartyMemberItem> PartyMembers { get; set; } = new();
    public List<GuildMemberItem> GuildMembers { get; set; } = new();
    public List<NoteItem> Notes { get; set; } = new();
    public List<NotificationItem> Notifications { get; set; } = new();
    public List<LecturerVerificationRequestItem> LecturerVerificationRequests { get; set; } = new();
    public List<QuestAttemptItem> QuestAttempts { get; set; } = new();
}

public class CountsSection
{
    public int Notes { get; set; }
    public int Achievements { get; set; }
    public int Meetings { get; set; }
    public int NotificationsUnread { get; set; }
    public int QuestsCompleted { get; set; }
    public int QuestsInProgress { get; set; }
}

public record UserRoleItem(Guid RoleId, DateTimeOffset AssignedAt, string? RoleName);
public record StudentEnrollmentItem(Guid Id, string Status, DateOnly EnrollmentDate, DateOnly? ExpectedGraduationDate);
public record StudentTermSubjectItem(Guid Id, Guid SubjectId, string SubjectCode, string SubjectName, int? Semester, string Status, string? Grade);
public record UserSkillItem(Guid Id, string SkillName, int Level, int ExperiencePoints);
public record UserAchievementItem(Guid AchievementId, DateTimeOffset EarnedAt, string? AchievementName);
public record PartyMemberItem(Guid PartyId, string PartyName, string Role, DateTimeOffset? JoinedAt);
public record GuildMemberItem(Guid GuildId, string GuildName, string Role, DateTimeOffset? JoinedAt);
public record NoteItem(Guid Id, string Title, DateTimeOffset CreatedAt);
public record NotificationItem(Guid Id, string Type, string Title, bool IsRead, DateTimeOffset CreatedAt);
public record LecturerVerificationRequestItem(Guid Id, string Status, DateTimeOffset? SubmittedAt);
public record QuestAttemptItem(Guid AttemptId, Guid QuestId, string QuestTitle, string Status, decimal CompletionPercentage, int TotalExperienceEarned, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt, int StepsTotal, int StepsCompleted, Guid? CurrentStepId);