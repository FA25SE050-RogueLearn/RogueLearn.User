using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Student.Commands.ProcessAcademicRecord;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Student.Commands.ProcessAcademicRecord;

public class ProcessAcademicRecordCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_UserProfileMissing_Throws(ProcessAcademicRecordCommand cmd)
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

        userProfileRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((RogueLearn.User.Domain.Entities.UserProfile?)null);

        var sut = new ProcessAcademicRecordCommandHandler(
            fap, enrollRepo, semSubjRepo, subjRepo, programRepo, programSubjRepo, classSpecSubjRepo, userProfileRepo,
            htmlSvc, logger, mediator, storage, bg, questRepo, stepGenSvc, skillMappingRepo, gradeXpCalc);

        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}