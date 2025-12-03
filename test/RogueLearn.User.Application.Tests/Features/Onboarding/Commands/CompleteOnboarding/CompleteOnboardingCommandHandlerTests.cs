using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Onboarding.Commands.CompleteOnboarding;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Onboarding.Commands.CompleteOnboarding;

public class CompleteOnboardingCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_UserMissing_Throws(CompleteOnboardingCommand cmd)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteOnboardingCommandHandler>>();
        var sut = new CompleteOnboardingCommandHandler(userRepo, programRepo, classRepo, logger);

        userRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns((UserProfile?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_AlreadyCompleted_Throws(CompleteOnboardingCommand cmd)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteOnboardingCommandHandler>>();
        var sut = new CompleteOnboardingCommandHandler(userRepo, programRepo, classRepo, logger);

        var profile = new UserProfile { AuthUserId = cmd.AuthUserId, OnboardingCompleted = true, RouteId = Guid.NewGuid(), ClassId = Guid.NewGuid() };
        userRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(profile);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_InvalidProgram_Throws(CompleteOnboardingCommand cmd)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteOnboardingCommandHandler>>();
        var sut = new CompleteOnboardingCommandHandler(userRepo, programRepo, classRepo, logger);

        userRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = cmd.AuthUserId });
        programRepo.ExistsAsync(cmd.CurriculumProgramId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_InvalidCareerRoadmap_Throws(CompleteOnboardingCommand cmd)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteOnboardingCommandHandler>>();
        var sut = new CompleteOnboardingCommandHandler(userRepo, programRepo, classRepo, logger);

        userRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(new UserProfile { AuthUserId = cmd.AuthUserId });
        programRepo.ExistsAsync(cmd.CurriculumProgramId, Arg.Any<CancellationToken>()).Returns(true);
        classRepo.ExistsAsync(cmd.CareerRoadmapId, Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_UpdatesProfile(CompleteOnboardingCommand cmd)
    {
        var userRepo = Substitute.For<IUserProfileRepository>();
        var programRepo = Substitute.For<ICurriculumProgramRepository>();
        var classRepo = Substitute.For<IClassRepository>();
        var logger = Substitute.For<Microsoft.Extensions.Logging.ILogger<CompleteOnboardingCommandHandler>>();
        var sut = new CompleteOnboardingCommandHandler(userRepo, programRepo, classRepo, logger);

        var profile = new UserProfile { AuthUserId = cmd.AuthUserId, OnboardingCompleted = false };
        userRepo.GetByAuthIdAsync(cmd.AuthUserId, Arg.Any<CancellationToken>()).Returns(profile);
        programRepo.ExistsAsync(cmd.CurriculumProgramId, Arg.Any<CancellationToken>()).Returns(true);
        classRepo.ExistsAsync(cmd.CareerRoadmapId, Arg.Any<CancellationToken>()).Returns(true);

        await sut.Handle(cmd, CancellationToken.None);
        await userRepo.Received(1).UpdateAsync(Arg.Is<UserProfile>(p => p.OnboardingCompleted == true && p.RouteId == cmd.CurriculumProgramId && p.ClassId == cmd.CareerRoadmapId), Arg.Any<CancellationToken>());
    }
}