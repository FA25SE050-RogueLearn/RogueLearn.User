using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserFullInfo.Queries.GetFullUserInfo;

public class GetFullUserInfoQueryHandlerTests
{
    [Fact]
    public async Task Handle_OrdersStudentTermSubjectsBySemester_WithNullLast()
    {
        var authId = Guid.NewGuid();

        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var enrollmentRepo = Substitute.For<IStudentEnrollmentRepository>();
        var termSubjectRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var userSkillRepo = Substitute.For<IUserSkillRepository>();
        var userAchievementRepo = Substitute.For<IUserAchievementRepository>();
        var achievementRepo = Substitute.For<IAchievementRepository>();
        var partyMemberRepo = Substitute.For<IPartyMemberRepository>();
        var guildMemberRepo = Substitute.For<IGuildMemberRepository>();
        var partyRepo = Substitute.For<IPartyRepository>();
        var guildRepo = Substitute.For<IGuildRepository>();
        var meetingRepo = Substitute.For<IMeetingParticipantRepository>();
        var noteRepo = Substitute.For<INoteRepository>();
        var noteTagRepo = Substitute.For<INoteTagRepository>();
        var notificationRepo = Substitute.For<INotificationRepository>();
        var verifRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        var questRepo = Substitute.For<IQuestRepository>();
        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var stepProgressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        var questStepRepo = Substitute.For<IQuestStepRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var rpc = Substitute.For<IRpcFullUserInfoService>();
        var logger = Substitute.For<ILogger<GetFullUserInfoQueryHandler>>();

        var profile = new UserProfile { AuthUserId = authId, Username = "u", Email = "e" };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        rpc.GetAsync(authId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((FullUserInfoResponse?)null);

        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserRole>());
        enrollmentRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<StudentEnrollment>());
        userSkillRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserSkill>());
        userAchievementRepo.FindPagedAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserAchievement>());
        partyMemberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<PartyMember>());
        guildMemberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<GuildMember>());
        noteRepo.FindPagedAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Note, bool>>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Note>());
        notificationRepo.GetLatestByUserAsync(authId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<Notification>());
        verifRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<LecturerVerificationRequest>());
        attemptRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<UserQuestAttempt>());
        meetingRepo.GetByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<MeetingParticipant>());

        var s1Id = Guid.NewGuid();
        var s2Id = Guid.NewGuid();
        var s3Id = Guid.NewGuid();
        var termSubjects = new List<StudentSemesterSubject>
        {
            new() { Id = Guid.NewGuid(), AuthUserId = authId, SubjectId = s1Id, Status = SubjectEnrollmentStatus.Passed, Grade = "8" },
            new() { Id = Guid.NewGuid(), AuthUserId = authId, SubjectId = s2Id, Status = SubjectEnrollmentStatus.Passed, Grade = "7" },
            new() { Id = Guid.NewGuid(), AuthUserId = authId, SubjectId = s3Id, Status = SubjectEnrollmentStatus.Passed, Grade = "6" }
        };
        termSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(termSubjects);

        subjectRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Subject>
        {
            new() { Id = s1Id, SubjectCode = "S1", SubjectName = "Subject 1", Semester = 2 },
            new() { Id = s2Id, SubjectCode = "S2", SubjectName = "Subject 2", Semester = 1 },
            new() { Id = s3Id, SubjectCode = "S3", SubjectName = "Subject 3", Semester = null }
        });

        var sut = new GetFullUserInfoQueryHandler(
            userProfileRepo,
            userRoleRepo,
            roleRepo,
            enrollmentRepo,
            termSubjectRepo,
            userSkillRepo,
            userAchievementRepo,
            achievementRepo,
            partyMemberRepo,
            guildMemberRepo,
            partyRepo,
            guildRepo,
            meetingRepo,
            noteRepo,
            noteTagRepo,
            notificationRepo,
            verifRepo,
            questRepo,
            attemptRepo,
            stepProgressRepo,
            questStepRepo,
            subjectRepo,
            classRepo,
            programRepo,
            rpc,
            logger
        );
    }

    [Fact]
    public async Task Handle_ProfileNotFound_ReturnsNull()
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var authId = Guid.NewGuid();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        var sut = CreateSut(userProfileRepo: userRepo);
        var res = await sut.Handle(new GetFullUserInfoQuery { AuthUserId = authId }, CancellationToken.None);
        res.Should().BeNull();
    }

    [Fact]
    public async Task Handle_RpcReturns_ReturnsRpcResponse()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, Username = "u", Email = "e@x.com" };
        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        var rpc = Substitute.For<IRpcFullUserInfoService>();
        var rpcResponse = new FullUserInfoResponse { Profile = new ProfileSection { AuthUserId = authId, Username = "u", Email = "e@x.com" } };
        rpc.GetAsync(authId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(rpcResponse);
        var sut = CreateSut(userProfileRepo: userRepo, rpc: rpc);
        var res = await sut.Handle(new GetFullUserInfoQuery { AuthUserId = authId }, CancellationToken.None);
        res.Should().BeSameAs(rpcResponse);
    }

    [Fact]
    public async Task Handle_RpcNull_BuildsFallbackSections()
    {
        var authId = Guid.NewGuid();
        var profile = new UserProfile {
            Id = Guid.NewGuid(), AuthUserId = authId, Username = "u", Email = "e@x.com",
            FirstName = "F", LastName = "L", Level = 2, ExperiencePoints = 100,
            ClassId = Guid.NewGuid(), RouteId = Guid.NewGuid(), CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow,
            ProfileImageUrl = "https://cdn/u.png", OnboardingCompleted = true
        };

        var userRepo = Substitute.For<IUserProfileRepository>();
        userRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);

        var rpc = Substitute.For<IRpcFullUserInfoService>();
        rpc.GetAsync(authId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns((FullUserInfoResponse?)null);

        var classRepo = Substitute.For<IClassRepository>();
        classRepo.GetByIdAsync(profile.ClassId!.Value, Arg.Any<CancellationToken>()).Returns(new Class { Id = profile.ClassId!.Value, Name = "Class A" });
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        programRepo.GetByIdAsync(profile.RouteId!.Value, Arg.Any<CancellationToken>()).Returns(new CurriculumProgram { Id = profile.RouteId!.Value, ProgramName = "Prog X" });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleId = Guid.NewGuid();
        userRoleRepo.GetRolesForUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { AuthUserId = authId, RoleId = roleId, AssignedAt = DateTimeOffset.UtcNow } });
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Role> { new() { Id = roleId, Name = "Member" } });

        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        enrollRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentEnrollment> { new() { Id = Guid.NewGuid(), Status = EnrollmentStatus.Active, EnrollmentDate = new DateOnly(2024, 1, 1), ExpectedGraduationDate = new DateOnly(2026, 6, 1) } });

        var termSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjId = Guid.NewGuid();
        termSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject> { new() { Id = Guid.NewGuid(), AuthUserId = authId, SubjectId = subjId, Status = SubjectEnrollmentStatus.Studying, Grade = "A" } });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Subject> { new() { Id = subjId, SubjectCode = "CS101", SubjectName = "Intro", Semester = 1 } });

        var skillRepo = Substitute.For<IUserSkillRepository>();
        skillRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserSkill, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserSkill> { new() { Id = Guid.NewGuid(), AuthUserId = authId, SkillName = "C#", Level = 3, ExperiencePoints = 200 } });

        var userAchRepo = Substitute.For<IUserAchievementRepository>();
        var achId = Guid.NewGuid();
        userAchRepo.FindPagedAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserAchievement> { new() { Id = Guid.NewGuid(), AuthUserId = authId, AchievementId = achId, EarnedAt = DateTimeOffset.UtcNow } });
        var achRepo = Substitute.For<IAchievementRepository>();
        achRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Achievement> { new() { Id = achId, Name = "Winner" } });

        var partyMemberRepo = Substitute.For<IPartyMemberRepository>();
        var partyId = Guid.NewGuid();
        partyMemberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<PartyMember> { new() { PartyId = partyId, Role = PartyRole.Member, JoinedAt = DateTimeOffset.UtcNow } });
        var partyRepo = Substitute.For<IPartyRepository>();
        partyRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Party> { new() { Id = partyId, Name = "Team Alpha" } });

        var guildMemberRepo = Substitute.For<IGuildMemberRepository>();
        var guildId = Guid.NewGuid();
        guildMemberRepo.GetMembershipsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember> { new() { GuildId = guildId, Role = GuildRole.Member, JoinedAt = DateTimeOffset.UtcNow } });
        var guildRepo = Substitute.For<IGuildRepository>();
        guildRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new List<Guild> { new() { Id = guildId, Name = "Guild Z" } });

        var noteRepo = Substitute.For<INoteRepository>();
        noteRepo.FindPagedAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Note, bool>>>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new List<Note> { new() { Id = Guid.NewGuid(), Title = "Note 1", CreatedAt = DateTimeOffset.UtcNow, AuthUserId = authId } });

        var notiRepo = Substitute.For<INotificationRepository>();
        notiRepo.GetLatestByUserAsync(authId, Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(new List<Notification> { new() { Id = Guid.NewGuid(), Type = NotificationType.System, Title = "Hello", IsRead = false, CreatedAt = DateTimeOffset.UtcNow, AuthUserId = authId } });

        var verifRepo = Substitute.For<ILecturerVerificationRequestRepository>();
        verifRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<LecturerVerificationRequest, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<LecturerVerificationRequest> { new() { Id = Guid.NewGuid(), Status = VerificationStatus.Pending, SubmittedAt = DateTimeOffset.UtcNow } });

        var attemptRepo = Substitute.For<IUserQuestAttemptRepository>();
        var questId = Guid.NewGuid();
        var attemptId = Guid.NewGuid();
        attemptRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserQuestAttempt, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<UserQuestAttempt> { new() { Id = attemptId, AuthUserId = authId, QuestId = questId, Status = QuestAttemptStatus.Completed, CompletionPercentage = 1.0m, TotalExperienceEarned = 50, StartedAt = DateTimeOffset.UtcNow.AddDays(-1), CompletedAt = DateTimeOffset.UtcNow } });

        var questRepo = Substitute.For<IQuestRepository>();
        questRepo.GetByIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Quest> { new() { Id = questId, Title = "Quest X" } });

        var stepRepo = Substitute.For<IQuestStepRepository>();
        stepRepo.GetByQuestIdAsync(questId, Arg.Any<CancellationToken>()).Returns(new List<QuestStep> { new() { Id = Guid.NewGuid(), QuestId = questId }, new() { Id = Guid.NewGuid(), QuestId = questId }, new() { Id = Guid.NewGuid(), QuestId = questId } });

        var stepProgressRepo = Substitute.For<IUserQuestStepProgressRepository>();
        stepProgressRepo.GetCompletedStepsCountForAttemptAsync(attemptId, Arg.Any<CancellationToken>()).Returns(2);

        var meetingRepo = Substitute.For<IMeetingParticipantRepository>();
        meetingRepo.GetByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(Enumerable.Empty<MeetingParticipant>());

        // counts
        noteRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Note, bool>>>(), Arg.Any<CancellationToken>()).Returns(1);
        userAchRepo.CountAsync(Arg.Any<System.Linq.Expressions.Expression<Func<UserAchievement, bool>>>(), Arg.Any<CancellationToken>()).Returns(1);
        notiRepo.CountUnreadByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(5);

        var sut = CreateSut(
            userProfileRepo: userRepo,
            userRoleRepo: userRoleRepo,
            roleRepo: roleRepo,
            enrollRepo: enrollRepo,
            termSubjRepo: termSubjRepo,
            userSkillRepo: skillRepo,
            userAchRepo: userAchRepo,
            achRepo: achRepo,
            partyMemberRepo: partyMemberRepo,
            guildMemberRepo: guildMemberRepo,
            partyRepo: partyRepo,
            guildRepo: guildRepo,
            meetingRepo: meetingRepo,
            noteRepo: noteRepo,
            noteTagRepo: Substitute.For<INoteTagRepository>(),
            notiRepo: notiRepo,
            verifRepo: verifRepo,
            questRepo: questRepo,
            attemptRepo: attemptRepo,
            stepProgressRepo: stepProgressRepo,
            stepRepo: stepRepo,
            subjectRepo: subjectRepo,
            classRepo: classRepo,
            programRepo: programRepo,
            rpc: rpc,
            logger: Substitute.For<ILogger<GetFullUserInfoQueryHandler>>()
        );

        var res = await sut.Handle(new GetFullUserInfoQuery { AuthUserId = authId, PageNumber = 1, PageSize = 10 }, CancellationToken.None);
        res.Should().NotBeNull();
        var ordered = res!.Relations.StudentTermSubjects.Select(s => s.SubjectCode).ToList();
        ordered.Should().Equal("S2", "S1", "S3");
    }
}
