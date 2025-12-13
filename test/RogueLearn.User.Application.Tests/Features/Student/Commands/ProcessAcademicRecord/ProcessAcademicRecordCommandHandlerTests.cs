using System.Text.Json;
using FluentAssertions;
using MediatR;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestLineFromCurriculum;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Features.UserSkillRewards.Commands.IngestXpEvent;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommandHandlerTests
{
    private static ProcessAcademicRecordCommandHandler CreateSut(
        IFapExtractionPlugin? fap = null,
        IStudentEnrollmentRepository? enrollRepo = null,
        IStudentSemesterSubjectRepository? semSubRepo = null,
        ISubjectRepository? subjectRepo = null,
        ICurriculumProgramRepository? programRepo = null,
        ICurriculumProgramSubjectRepository? programSubjectRepo = null,
        IClassSpecializationSubjectRepository? classSubjectRepo = null,
        IUserProfileRepository? userRepo = null,
        IHtmlCleaningService? html = null,
        Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>? logger = null,
        IMediator? mediator = null,
        ICurriculumImportStorage? storage = null,
        Hangfire.IBackgroundJobClient? bg = null,
        IQuestRepository? questRepo = null,
        IQuestStepGenerationService? stepGen = null,
        ISubjectSkillMappingRepository? mappingRepo = null,
        IGradeExperienceCalculator? calc = null)
    {
        fap ??= Substitute.For<IFapExtractionPlugin>();
        enrollRepo ??= Substitute.For<IStudentEnrollmentRepository>();
        semSubRepo ??= Substitute.For<IStudentSemesterSubjectRepository>();
        subjectRepo ??= Substitute.For<ISubjectRepository>();
        programRepo ??= Substitute.For<ICurriculumProgramRepository>();
        programSubjectRepo ??= Substitute.For<ICurriculumProgramSubjectRepository>();
        classSubjectRepo ??= Substitute.For<IClassSpecializationSubjectRepository>();
        userRepo ??= Substitute.For<IUserProfileRepository>();
        html ??= Substitute.For<IHtmlCleaningService>();
        logger ??= Substitute.For<Microsoft.Extensions.Logging.ILogger<ProcessAcademicRecordCommandHandler>>();
        mediator ??= Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>())
            .Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        storage ??= Substitute.For<ICurriculumImportStorage>();
        bg ??= Substitute.For<Hangfire.IBackgroundJobClient>();
        questRepo ??= Substitute.For<IQuestRepository>();
        stepGen ??= Substitute.For<IQuestStepGenerationService>();
        mappingRepo ??= Substitute.For<ISubjectSkillMappingRepository>();
        calc ??= Substitute.For<IGradeExperienceCalculator>();

        return new ProcessAcademicRecordCommandHandler(
            fap,
            enrollRepo,
            semSubRepo,
            subjectRepo,
            programRepo,
            programSubjectRepo,
            classSubjectRepo,
            userRepo,
            html,
            logger,
            mediator,
            storage,
            bg,
            questRepo,
            stepGen,
            mappingRepo,
            calc);
    }

    [Fact]
    public async Task Handle_ClassSubjectsAdded_ToAllowedList()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 120) + " PRO192 7.5 <table>");
        var fap = Substitute.For<IFapExtractionPlugin>();
        var json = JsonSerializer.Serialize(new { Subjects = new[] { new { SubjectCode = "PRO192", Status = "passed", Mark = 8.0, Semester = 1, AcademicYear = "2024" } }, Gpa = 8.0 });
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var proSubject = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO192", Credits = 3, Semester = 1 };
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { proSubject });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<CurriculumProgramSubject>());
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new[] { proSubject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());

        var sut = CreateSut(fap: fap, storage: storage, html: html, programRepo: program, userRepo: user, subjectRepo: subjectRepo, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, semSubRepo: semSubRepo);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
    }
    [Fact]
    public async Task Handle_UserWithoutClass_ThrowsBadRequest()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = null });
        var sut = CreateSut(programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("User has not selected a specialization class*");
    }

    [Fact]
    public async Task Handle_ProgramNotFound_ThrowsNotFound()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5 fap";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_CleanTextTooShort_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 50));
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, html: html, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 fap";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The extracted content is too short*");
    }

    [Fact]
    public async Task Handle_CleanTextHasNoSubjectCodes_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 120) + " <table> notcodes fap grade");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, html: html, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("No subject codes were found*");
    }

    [Fact]
    public async Task Handle_AiExtractionEmpty_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 120) + " PRO192 7.5 <table>");
        var fap = Substitute.For<IFapExtractionPlugin>();
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(fap: fap, storage: storage, html: html, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The AI could not extract any data*");
    }

    [Fact]
    public async Task Handle_TooManyInvalidStatuses_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new object[] { new { SubjectCode = "AAA111", Status = "weird" }, new { SubjectCode = "BBB222", Status = "strange" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> AAA111 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The extracted data contains too many invalid status values*");
    }

    [Fact]
    public async Task Handle_NoCache_CallsSaveLatest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 120) + " PRO192 7.5 <table>");
        var fap = Substitute.For<IFapExtractionPlugin>();
        var json = JsonSerializer.Serialize(new { Subjects = new[] { new { SubjectCode = "PRO192", Status = "passed", Mark = 8.0, Semester = 1, AcademicYear = "2024" } } });
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var proSubject = new Subject { Id = Guid.NewGuid(), SubjectCode = "PRO192", Credits = 3, Semester = 1 };
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { proSubject });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { new CurriculumProgramSubject { SubjectId = proSubject.Id } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());

        var sut = CreateSut(fap: fap, storage: storage, html: html, programRepo: program, userRepo: user, subjectRepo: subjectRepo, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, semSubRepo: semSubRepo);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await storage.Received(1).SaveLatestAsync("academic-records", Arg.Any<string>(), "fap-sync", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
    [Fact]
    public async Task Handle_EmptyHtml_ThrowsBadRequest()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = "" }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The provided HTML content is empty*");
    }

    [Fact]
    public async Task Handle_HtmlTooShort_ThrowsBadRequest()
    {
        var sut = CreateSut();
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = "abc" }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The provided content is too short*");
    }

    [Fact]
    public async Task Handle_HtmlMissingTable_ThrowsBadRequest()
    {
        var content = new string('x', 600) + " fap";
        var sut = CreateSut();
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = content }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The provided content does not appear to be a valid HTML table*");
    }

    [Fact]
    public async Task Handle_NotFapIndicators_ThrowsBadRequest()
    {
        var content = new string('x', 600) + "<table>";
        var sut = CreateSut();
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = content }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The provided HTML does not appear to be from the FAP academic portal*");
    }

    [Fact]
    public async Task Handle_CleanTextEmpty_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns("");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, html: html, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Failed to extract readable content*");
    }

    [Fact]
    public async Task Handle_AiExtractionInvalidJson_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        var html = Substitute.For<IHtmlCleaningService>();
        html.ExtractCleanTextFromHtml(Arg.Any<string>()).Returns(new string('x', 120) + " PRO192 7.5 <table>");
        var fap = Substitute.For<IFapExtractionPlugin>();
        fap.ExtractFapRecordJsonAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("[]");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(fap: fap, storage: storage, html: html, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The AI returned an invalid response*");
    }

    [Fact]
    public async Task Handle_CachedJsonParseError_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("{");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The AI failed to extract valid data*");
    }

    [Fact]
    public async Task Handle_NullDeserializedFapData_ThrowsBadRequest()
    {
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("null");
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("Failed to parse the extracted academic data*");
    }

    [Fact]
    public async Task Handle_NoSubjectsFound_ThrowsBadRequest()
    {
        var json = JsonSerializer.Serialize(new { Gpa = 8.5, Subjects = Array.Empty<object>() });
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("No subjects were found*");
    }

    [Fact]
    public async Task Handle_AllSubjectsMissingCodes_ThrowsBadRequest()
    {
        var json = JsonSerializer.Serialize(new
        {
            Subjects = new[] { new { SubjectCode = "" }, new { SubjectCode = "" } }
        });
        var storage = Substitute.For<ICurriculumImportStorage>();
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = Guid.NewGuid() });
        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<BadRequestException>().WithMessage("The extracted subjects are missing subject codes*");
    }

    [Fact]
    public async Task Handle_PartialInvalidSubjects_Continues_AndSuccess()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId, SubjectCode = "PRO192", Credits = 3, Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var json = "{\"gpa\":8.5,\"subjects\":[{\"subjectCode\":\"\",\"subjectName\":\"X\",\"status\":\"passed\",\"mark\":8.0,\"semester\":1,\"academicYear\":\"2024\"},{\"subjectCode\":\"PRO192\",\"subjectName\":\"Programming\",\"status\":\"passed\",\"mark\":8.0,\"semester\":1,\"academicYear\":\"2024\"}]}";
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(new IngestXpEventResponse { Processed = false, SkillName = "Skill", NewExperiencePoints = 0, NewLevel = 0 });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subject.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> PRO192 7.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.IsSuccess.Should().BeTrue();
        res.SubjectsProcessed.Should().Be(2);
        res.XpAwarded.Should().BeNull();
        await semSubRepo.Received(1).AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubjectNotInCatalog_Ignores_AndNoXpAward()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId2 = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId2 } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjId2, SubjectCode = "MATH101", Credits = 3, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "PHYS101", SubjectName = "Physics", Status = "passed", Mark = 9.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(new IngestXpEventResponse { Processed = true, SkillName = "Skill", NewExperiencePoints = 100, NewLevel = 1 });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subjId2, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> MATH101 7.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.XpAwarded.Should().BeNull();
        await semSubRepo.Received(0).AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAllowedSubjects_ThrowsNotFound()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<CurriculumProgramSubject>());
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "ANY", SubjectName = "Any", Status = "passed", Mark = 9.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, mediator: mediator);
        var htmlContent = new string('x', 600) + "<table> ANY101 9.0";
        var act = () => sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>().WithMessage("No subjects are associated*");
    }

    [Fact]
    public async Task Handle_UpdateExistingRecord_ChangesGradeAndStatus()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        var authId = Guid.NewGuid();
        user.GetByAuthIdAsync(authId, Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = authId, ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjId, SubjectCode = "PROZ", Credits = 3, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        var existing = new StudentSemesterSubject { AuthUserId = authId, SubjectId = subjId, Grade = "6.0", Status = SubjectEnrollmentStatus.Studying, AcademicYear = "2024" };
        semSubRepo.GetSemesterSubjectsByUserAsync(authId, Arg.Any<CancellationToken>()).Returns(new List<StudentSemesterSubject> { existing });
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "PROZ", SubjectName = "Z", Status = "passed", Mark = 8.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> PROZ101 8.0";
        StudentSemesterSubject? updated = null;
        semSubRepo.UpdateAsync(Arg.Do<StudentSemesterSubject>(x => updated = x), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = authId, CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        updated.Should().NotBeNull();
        updated!.Grade.Should().Be("8.0");
        updated.Status.Should().Be(SubjectEnrollmentStatus.Passed);
        updated.CreditsEarned.Should().Be(3);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_XpAwarding_ProcessedTrue_AddsSummary()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId, SubjectCode = "AAA111", Credits = 3, Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "AAA111", SubjectName = "Aaa", Status = "passed", Mark = 9.5, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(new IngestXpEventResponse { Processed = true, SkillName = "Skill", NewExperiencePoints = 200, NewLevel = 2 });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subject.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });
        var calc = Substitute.For<IGradeExperienceCalculator>();
        calc.GetTierInfo(Arg.Any<int>()).Returns((2, 2000, "Tier2"));
        calc.CalculateXpAward(Arg.Any<double>(), Arg.Any<int>(), Arg.Any<decimal>()).Returns(200);

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo, calc: calc);
        var htmlContent = new string('x', 600) + "<table> AAA111 9.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.XpAwarded.Should().NotBeNull();
        res.XpAwarded!.SkillAwards.First().NewLevel.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ExcludedBySubjectCode_Skips()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjIdExcluded = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjIdExcluded } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjIdExcluded, SubjectCode = "VOV114", Credits = 1, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "VOV114", SubjectName = "Vovinam", Status = "passed", Mark = 9.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> VOV114 9.0";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await semSubRepo.Received(0).AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>());
        res.XpAwarded.Should().BeNull();
    }

    [Fact]
    public async Task Handle_FallbackStatusFromMark_SetsNotPassed()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjId, SubjectCode = "BBB222", Credits = 2, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "BBB222", SubjectName = "Bbb", Status = "", Mark = 4.5, Semester = 1, AcademicYear = "2024" }, new { SubjectCode = "BBB222", SubjectName = "Bbb", Status = "passed", Mark = 7.5, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var added = new List<StudentSemesterSubject>();
        semSubRepo.AddAsync(Arg.Do<StudentSemesterSubject>(x => added.Add(x)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> BBB222 4.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        added[0].Status.Should().Be(SubjectEnrollmentStatus.NotPassed);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_FallbackStatusFromMark_SetsPassed()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjId, SubjectCode = "CCC333", Credits = 2, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "CCC333", SubjectName = "Ccc", Status = "", Mark = 8.5, Semester = 1, AcademicYear = "2024" }, new { SubjectCode = "CCC333", SubjectName = "Ccc", Status = "passed", Mark = 7.5, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var added = new List<StudentSemesterSubject>();
        semSubRepo.AddAsync(Arg.Do<StudentSemesterSubject>(x => added.Add(x)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> CCC333 8.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        added[0].Status.Should().Be(SubjectEnrollmentStatus.Passed);
        res.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MapFapStatusVariants_CreatesRecordsWithCorrectStatus()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId3 = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId3 } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId3, SubjectCode = "PROV", Credits = 3, Semester = 2 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new
        {
            Subjects = new object[]
            {
                new { SubjectCode = "PROV", SubjectName = "Prov", Status = "pass", Mark = 8.0, Semester = 2, AcademicYear = "2022" },
                new { SubjectCode = "PROV", SubjectName = "Prov", Status = "fail", Mark = 4.0, Semester = 2, AcademicYear = "2023" },
                new { SubjectCode = "PROV", SubjectName = "Prov", Status = "in progress", Mark = (double?)null, Semester = 2, AcademicYear = "2024" },
                new { SubjectCode = "PROV", SubjectName = "Prov", Status = "not started", Mark = (double?)null, Semester = 2, AcademicYear = "2025" }
            }
        };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var added = new List<StudentSemesterSubject>();
        semSubRepo.AddAsync(Arg.Do<StudentSemesterSubject>(x => added.Add(x)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> PROV101 7.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        added.Should().HaveCount(4);
        added[0].Status.Should().Be(SubjectEnrollmentStatus.Passed);
        added[0].CreditsEarned.Should().Be(3);
        added[1].Status.Should().Be(SubjectEnrollmentStatus.NotPassed);
        added[2].Status.Should().Be(SubjectEnrollmentStatus.Studying);
        added[3].Status.Should().Be(SubjectEnrollmentStatus.NotStarted);
        res.XpAwarded.Should().BeNull();
    }

    [Fact]
    public async Task AwardXp_NoPassedSubjects_ReturnsZero()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjIdMap = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjIdMap } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjIdMap, SubjectCode = "PROX", Credits = 3, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "PROX", SubjectName = "X", Status = "fail", Mark = 4.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> PROX101 4.0";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.XpAwarded.Should().BeNull();
    }

    [Fact]
    public async Task AwardXp_AlreadyAwarded_SkipsAddingToSummary()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId4 = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId4 } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId4, SubjectCode = "ABC123", Credits = 3, Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "ABC123", SubjectName = "A", Status = "passed", Mark = 9.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        mediator.Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>()).Returns(new IngestXpEventResponse { Processed = false, SkillName = "Skill", NewExperiencePoints = 0, NewLevel = 0 });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());
        var calc = Substitute.For<IGradeExperienceCalculator>();
        calc.GetTierInfo(Arg.Any<int>()).Returns((1, 1500, "Tier"));
        calc.CalculateXpAward(Arg.Any<double>(), Arg.Any<int>(), Arg.Any<decimal>()).Returns(100);

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo, calc: calc);
        var htmlContent = new string('x', 600) + "<table> ABC123 9.0";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.XpAwarded.Should().BeNull();
        // XP ingestion is attempted during processing; when already awarded, response is Processed=false and summary remains null.
    }

    [Fact]
    public async Task Handle_StatusMapping_Studying_NotPassed_Unknown()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId, SubjectCode = "MAP101", Credits = 3, Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new
        {
            Subjects = new object[]
            {
                new { SubjectCode = "MAP101", SubjectName = "X", Status = "studying", Mark = (double?)null, Semester = 1, AcademicYear = "2023" },
                new { SubjectCode = "MAP101", SubjectName = "X", Status = "not passed", Mark = 4.0, Semester = 1, AcademicYear = "2024" },
                new { SubjectCode = "MAP101", SubjectName = "X", Status = "unknown-status", Mark = (double?)null, Semester = 1, AcademicYear = "2025" }
            }
        };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var added = new List<StudentSemesterSubject>();
        semSubRepo.AddAsync(Arg.Do<StudentSemesterSubject>(x => added.Add(x)), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<StudentSemesterSubject>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> MAP101 7.5";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        added.Should().HaveCount(3);
        added[0].Status.Should().Be(SubjectEnrollmentStatus.Studying);
        added[1].Status.Should().Be(SubjectEnrollmentStatus.NotPassed);
        added[2].Status.Should().Be(SubjectEnrollmentStatus.NotStarted);
        res.XpAwarded.Should().BeNull();
    }

    [Fact]
    public async Task AwardXp_PassedWithoutMark_Skips()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjId = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjId } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subject = new Subject { Id = subjId, SubjectCode = "ABC123", Credits = 3, Semester = 1 };
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { subject });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var json2 = "{\"subjects\":[{\"subjectCode\":\"ABC123\",\"subjectName\":\"A\",\"status\":\"passed\",\"mark\":null,\"semester\":1,\"academicYear\":\"2024\"}]}";
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(json2);
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(new[] { new SubjectSkillMapping { SubjectId = subject.Id, SkillId = Guid.NewGuid(), RelevanceWeight = 1.0m } });
        var calc = Substitute.For<IGradeExperienceCalculator>();
        calc.GetTierInfo(Arg.Any<int>()).Returns((1, 1500, "Tier"));
        calc.CalculateXpAward(Arg.Any<double>(), Arg.Any<int>(), Arg.Any<decimal>()).Returns(100);

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo, calc: calc);
        var htmlContent = new string('x', 600) + "<table> ABC123 9.0";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        res.XpAwarded.Should().BeNull();
        await mediator.DidNotReceive().Send(Arg.Any<IngestXpEventCommand>(), Arg.Any<CancellationToken>());
    }


    [Fact]
    public async Task Handle_ExcludedBySubjectName_Skips()
    {
        var program = Substitute.For<ICurriculumProgramRepository>();
        program.ExistsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<IUserProfileRepository>();
        var classId = Guid.NewGuid();
        user.GetByAuthIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = Guid.NewGuid(), ClassId = classId });
        var programSubjectRepo = Substitute.For<ICurriculumProgramSubjectRepository>();
        var subjIdExcluded = Guid.NewGuid();
        programSubjectRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<CurriculumProgramSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(new[] { new CurriculumProgramSubject { ProgramId = Guid.NewGuid(), SubjectId = subjIdExcluded } });
        var classSubjectRepo = Substitute.For<IClassSpecializationSubjectRepository>();
        classSubjectRepo.GetSubjectByClassIdAsync(classId, Arg.Any<CancellationToken>()).Returns(Array.Empty<Subject>());
        var subjectRepo = Substitute.For<ISubjectRepository>();
        subjectRepo.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new[] { new Subject { Id = subjIdExcluded, SubjectCode = "ABC123", Credits = 3, Semester = 1 } });
        var semSubRepo = Substitute.For<IStudentSemesterSubjectRepository>();
        semSubRepo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<StudentSemesterSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<StudentSemesterSubject>());
        var storage = Substitute.For<ICurriculumImportStorage>();
        var data = new { Subjects = new[] { new { SubjectCode = "ABC123", SubjectName = "Vovinam training", Status = "passed", Mark = 9.0, Semester = 1, AcademicYear = "2024" } } };
        storage.TryGetByHashJsonAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(JsonSerializer.Serialize(data));
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GenerateQuestLine>(), Arg.Any<CancellationToken>()).Returns(new GenerateQuestLineResponse { LearningPathId = Guid.NewGuid() });
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        mappingRepo.GetMappingsBySubjectIdsAsync(Arg.Any<IEnumerable<Guid>>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SubjectSkillMapping>());

        var sut = CreateSut(storage: storage, programRepo: program, userRepo: user, programSubjectRepo: programSubjectRepo, classSubjectRepo: classSubjectRepo, subjectRepo: subjectRepo, semSubRepo: semSubRepo, mediator: mediator, mappingRepo: mappingRepo);
        var htmlContent = new string('x', 600) + "<table> ABC123 9.0";
        var res = await sut.Handle(new ProcessAcademicRecordCommand { AuthUserId = Guid.NewGuid(), CurriculumProgramId = Guid.NewGuid(), FapHtmlContent = htmlContent }, CancellationToken.None);
        await semSubRepo.Received(0).AddAsync(Arg.Any<StudentSemesterSubject>(), Arg.Any<CancellationToken>());
        res.XpAwarded.Should().BeNull();
    }
}
