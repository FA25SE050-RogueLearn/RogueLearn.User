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
            logger);

        var res = await sut.Handle(new GetFullUserInfoQuery { AuthUserId = authId, PageNumber = 1, PageSize = 10 }, CancellationToken.None);
        res.Should().NotBeNull();
        var ordered = res!.Relations.StudentTermSubjects.Select(s => s.SubjectCode).ToList();
        ordered.Should().Equal("S2", "S1", "S3");
    }
}
