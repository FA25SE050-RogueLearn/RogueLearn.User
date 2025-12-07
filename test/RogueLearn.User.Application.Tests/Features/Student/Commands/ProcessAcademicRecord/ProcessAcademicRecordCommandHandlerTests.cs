using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommandHandlerTests
{
    [Fact]
    public async Task Handle_UserProfileMissing_Throws()
    {
        var html = new string('X', 520) + "<table><tr><td>PRO192</td><td>8.0</td><td>FAP transcript</td></tr></table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), FapHtmlContent = html, CurriculumProgramId = Guid.NewGuid() };
        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        userProfileRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((RogueLearn.User.Domain.Entities.UserProfile?)null);

        mediator.Send(Arg.Any<GenerateQuestLine>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CreationPath_AddsNewSemesterSubject_AndAwardsXp()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " PRO200 6.5 grade table");
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"gpa\":7.5,\"subjects\":[{\"subjectCode\":\"PRO200\",\"status\":\"passed\",\"mark\":6.5,\"semester\":2,\"academicYear\":\"2025-2026\"}]}");

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO200", Credits = 3, Semester = 2 };
        subjRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject });
        programSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject> { new() { ProgramId = programId, SubjectId = subject.Id } });
        classSpecSubjRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(new List<Subject>());

        semSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        semSubjRepo.AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        skillMappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubjectSkillMapping> { new() { SubjectId = subject.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });
        gradeXpCalc.GetTierInfo(2).Returns((2, 1700, "Intermediate (Semester 4-6)"));
        gradeXpCalc.CalculateXpAward(6.5, 2, 1.0m).Returns(900);
        mediator.Send(Arg.Any<IngestXpEventCommand>()).Returns(new IngestXpEventResponse { Processed = true, SkillName = "Algo", NewExperiencePoints = 2000, NewLevel = 3 });
        mediator.Send(Arg.Any<GenerateQuestLine>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table>fap PRO200 6.5</table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        var res = await sut.Handle(cmd, CancellationToken.None);

        await semSubjRepo.Received(1).AddAsync(Arg.Is<StudentSemesterSubject>(ss => ss.SubjectId == subject.Id && ss.Status == SubjectEnrollmentStatus.Passed), Arg.Any<CancellationToken>());
        res.IsSuccess.Should().BeTrue();
        res.XpAwarded.Should().NotBeNull();
        res.XpAwarded!.TotalXp.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Handle_StatusEmptyWithMark_FallbacksToFailed()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " AAA101 4.0 grade table");
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"gpa\":5.0,\"subjects\":[{\"subjectCode\":\"AAA101\",\"status\":\"\",\"mark\":4.0,\"semester\":1,\"academicYear\":\"2025-2026\"},{\"subjectCode\":\"BBB102\",\"status\":\"passed\",\"mark\":7.0,\"semester\":1,\"academicYear\":\"2025-2026\"}]}");

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "AAA101", Credits = 2, Semester = 1 };
        var subjectB = new Subject { Id = Guid.NewGuid(), SubjectCode = "BBB102", Credits = 3, Semester = 1 };
        subjRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject, subjectB });
        programSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject> { new() { ProgramId = programId, SubjectId = subject.Id }, new() { ProgramId = programId, SubjectId = subjectB.Id } });
        classSpecSubjRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(new List<Subject>());

        semSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject>());
        semSubjRepo.AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        mediator.Send(Arg.Any<GenerateQuestLine>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table>fap AAA101 4.0</table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        var res = await sut.Handle(cmd, CancellationToken.None);

        await semSubjRepo.Received(1).AddAsync(Arg.Is<StudentSemesterSubject>(ss => ss.SubjectId == subject.Id && ss.Status == SubjectEnrollmentStatus.NotPassed), Arg.Any<CancellationToken>());
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AwardsXpAndReturnsSummary()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId, Username = "u" };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        var cleanText = new string('X', 300) + " PRO192 8.0 semester subject grade table";
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(cleanText);
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);

        var extractedJson = "{\"gpa\":8.0,\"subjects\":[{\"subjectCode\":\"PRO192\",\"status\":\"passed\",\"mark\":8.0,\"semester\":2,\"academicYear\":\"2025-2026\"}]}";
        fap.ExtractFapRecordJsonAsync(cleanText, Arg.Any<CancellationToken>()).Returns(extractedJson);

        var subject = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO192", SubjectName = "Programming", Credits = 3, Semester = 2 };
        subjRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject });
        programSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject> { new() { ProgramId = programId, SubjectId = subject.Id } });
        classSpecSubjRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(new List<Subject> { subject });

        semSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject>());

        skillMappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubjectSkillMapping> { new() { SubjectId = subject.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });

        gradeXpCalc.GetTierInfo(2).Returns((1, 1500, "Foundation (Semester 1-3)"));
        gradeXpCalc.CalculateXpAward(8.0, 2, 1.0m).Returns(1200);

        mediator.Send(Arg.Any<IngestXpEventCommand>()).Returns(new IngestXpEventResponse
        {
            Processed = true,
            SkillName = "Coding",
            NewExperiencePoints = 2200,
            NewLevel = 2
        });

        mediator.Send(Arg.Any<GenerateQuestLine>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table><tr><td>PRO192</td><td>8.0</td></tr></table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        var res = await sut.Handle(cmd, CancellationToken.None);

        res.IsSuccess.Should().BeTrue();
        res.XpAwarded.Should().NotBeNull();
        res.XpAwarded!.TotalXp.Should().Be(1200);
        res.XpAwarded.SkillsAffected.Should().Be(1);
        res.XpAwarded.SkillAwards.Should().HaveCount(1);
        res.XpAwarded.SkillAwards[0].SourceSubjectCode.Should().Be("PRO192");
        res.XpAwarded.SkillAwards[0].Grade.Should().Be("8.0");
    }

    [Fact]
    public async Task Handle_InvalidProgramId_ThrowsNotFound()
    {
        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var authId = Guid.NewGuid();
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        programRepo.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table><tr><td>FAP transcript</td><td>PRO192</td><td>8.0</td></tr></table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = Guid.NewGuid() };
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("fap transcript pro192 8.0 grade table");
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidHtmlContentTooShort_ThrowsBadRequest()
    {
        var html = new string('X', 120);
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), FapHtmlContent = html, CurriculumProgramId = Guid.NewGuid() };

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidCleanedTextTooShort_ThrowsBadRequest()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Y', 80));

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table><tr><td>FAP transcript</td><td>PRO192</td><td>8.0</td></tr></table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_CacheHit_UpdatesExistingRecord_SkipsFapExtraction()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        enrollRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentEnrollment, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new StudentEnrollment { Id = Guid.NewGuid(), AuthUserId = authId });

        var extractedJson = "{\"gpa\":7.5,\"subjects\":[{\"subjectCode\":\"PRO192\",\"status\":\"studying\",\"mark\":7.9,\"semester\":2,\"academicYear\":\"2025-2026\"},{\"subjectCode\":\"VOV114\",\"status\":\"passed\",\"mark\":7.0,\"semester\":1,\"academicYear\":\"2025-2026\"}]}";
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(extractedJson);

        var s1 = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO192", Credits = 3, Semester = 2 };
        var s2 = new Subject { Id = Guid.NewGuid(), SubjectCode = "VOV114", Credits = 2, Semester = 1 };
        subjRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Subject> { s1, s2 });

        programSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<CurriculumProgramSubject> { new() { ProgramId = programId, SubjectId = s1.Id }, new() { ProgramId = programId, SubjectId = s2.Id } });
        classSpecSubjRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(new List<Subject>());

        var existing = new StudentSemesterSubject { Id = Guid.NewGuid(), AuthUserId = authId, SubjectId = s1.Id, AcademicYear = "2025-2026", Status = SubjectEnrollmentStatus.NotStarted };
        semSubjRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<StudentSemesterSubject> { existing });

        skillMappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new List<SubjectSkillMapping> { new() { SubjectId = s1.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });
        gradeXpCalc.GetTierInfo(2).Returns((1, 1500, "Foundation (Semester 1-3)"));
        gradeXpCalc.CalculateXpAward(7.9, 2, 1.0m).Returns(1100);
        mediator.Send(Arg.Any<IngestXpEventCommand>()).Returns(new IngestXpEventResponse { Processed = false });
        mediator.Send(Arg.Any<GenerateQuestLine>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var html = new string('X', 520) + "<table><tr><td>FAP transcript</td><td>PRO192</td><td>8.0</td></tr></table>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        var res = await sut.Handle(cmd, CancellationToken.None);

        await fap.DidNotReceive().ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await semSubjRepo.Received(1).UpdateAsync(Arg.Is<StudentSemesterSubject>(ss => ss.SubjectId == s1.Id && ss.Status != SubjectEnrollmentStatus.NotStarted), Arg.Any<CancellationToken>());
        await semSubjRepo.DidNotReceive().AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>());
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_InvalidHtml_NoTable_ThrowsBadRequest()
    {
        var html = new string('X', 520) + "<div>no tables here</div>";
        var cmd = new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), FapHtmlContent = html, CurriculumProgramId = Guid.NewGuid() };
        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidHtml_NoFapIndicators_ThrowsBadRequest()
    {
        var html = new string('X', 520) + "<table><tr><td>nonsense text without indicators</td></tr></table>";
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = html, CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AiExtractionInvalidJson_ThrowsBadRequest()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " PRO192 7.5 grade table");
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("notjson");

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = new string('X', 520) + "<table></table>", CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExtractedDataSubjectsEmpty_ThrowsBadRequest()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " PRO192 7.5 grade table");
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"gpa\":8.0,\"subjects\":[]}");

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = new string('X', 520) + "<table></table>", CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AllSubjectsMissingCodes_ThrowsBadRequest()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " PRO192 7.5 grade table");
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{\"gpa\":7.0,\"subjects\":[{\"subjectCode\":\"\",\"status\":\"passed\",\"mark\":8.0,\"semester\":1,\"academicYear\":\"2025-2026\"}]} ");

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = new string('X', 520) + "<table></table>", CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InvalidStatuses_MoreThanHalf_ThrowsBadRequest()
    {
        var authId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var classId = Guid.NewGuid();

        var fap = Substitute.For<IFapExtractionPlugin>();
        var enrollRepo = Substitute.For<IStudentEnrollmentRepository>();
        var semSubjRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var subjRepo = Substitute.For<ISubjectRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var programSubjRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var classSpecSubjRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        var userProfileRepo = Substitute.For<IUserProfileRepository>();
        var htmlSvc = Substitute.For<IHtmlCleaningService>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        var mediator = Substitute.For<MediatR.IMediator>();
        var storage = Substitute.For<ICurriculumImportStorage>();
        var bg = Substitute.For<Hangfire.IBackgroundJobClient>();
        var questRepo = Substitute.For<IQuestRepository>();
        var stepGenSvc = Substitute.For<IQuestStepGenerationService>();
        var skillMappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var gradeXpCalc = Substitute.For<IGradeExperienceCalculator>();

        var profile = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authId, RouteId = programId, ClassId = classId };
        userProfileRepo.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(programId, Arg.Any<CancellationToken>()).Returns(true);

        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        htmlSvc.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('Z', 200) + " PRO192 7.5 grade table");
        var bad = "{\"gpa\":7.0,\"subjects\":[{\"subjectCode\":\"AAA101\",\"status\":\"unknown\",\"mark\":7.5,\"semester\":1,\"academicYear\":\"2025-2026\"},{\"subjectCode\":\"BBB102\",\"status\":\"unknown\",\"mark\":6.2,\"semester\":2,\"academicYear\":\"2025-2026\"}]}";
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(bad);

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        var cmd = new ProcessAcademicRecordCommand { AuthUserId = authId, FapHtmlContent = new string('X', 520) + "<table></table>", CurriculumProgramId = programId };
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
