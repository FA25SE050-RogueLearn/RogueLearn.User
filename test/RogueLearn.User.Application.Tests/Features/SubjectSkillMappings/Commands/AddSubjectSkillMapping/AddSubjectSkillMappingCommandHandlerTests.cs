using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.SubjectSkillMappings.Commands.AddSubjectSkillMapping;

public class AddSubjectSkillMappingCommandHandlerTests
{
    [Fact]
    public async Task Handle_SubjectMissing_Throws()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SkillMissing_Throws()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        skillRepo.ExistsAsync(cmd.SkillId, Arg.Any<CancellationToken>()).Returns(false);
        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_ExistingMapping_Throws()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        skillRepo.ExistsAsync(cmd.SkillId, Arg.Any<CancellationToken>()).Returns(true);
        mappingRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns(new SubjectSkillMapping { SubjectId = cmd.SubjectId, SkillId = cmd.SkillId });
        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        await Assert.ThrowsAsync<ConflictException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.7m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        skillRepo.ExistsAsync(cmd.SkillId, Arg.Any<CancellationToken>()).Returns(true);
        mappingRepo.FirstOrDefaultAsync(Arg.Any<System.Linq.Expressions.Expression<Func<SubjectSkillMapping, bool>>>(), Arg.Any<CancellationToken>())
                   .Returns((SubjectSkillMapping?)null);

        var created = new SubjectSkillMapping { Id = Guid.NewGuid(), SubjectId = cmd.SubjectId, SkillId = cmd.SkillId, RelevanceWeight = cmd.RelevanceWeight };
        mappingRepo.AddAsync(Arg.Any<SubjectSkillMapping>(), Arg.Any<CancellationToken>()).Returns(created);

        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.SubjectId.Should().Be(cmd.SubjectId);
        resp.SkillId.Should().Be(cmd.SkillId);
        resp.RelevanceWeight.Should().Be(cmd.RelevanceWeight);
    }

    [Fact]
    public async Task Handle_SubjectMissing_ThrowsNotFound()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_SkillMissing_ThrowsNotFound()
    {
        var mappingRepo = Substitute.For<ISubjectSkillMappingRepository>();
        var subjectRepo = Substitute.For<ISubjectRepository>();
        var skillRepo = Substitute.For<ISkillRepository>();
        var logger = Substitute.For<ILogger<AddSubjectSkillMappingCommandHandler>>();

        var cmd = new AddSubjectSkillMappingCommand { SubjectId = Guid.NewGuid(), SkillId = Guid.NewGuid(), RelevanceWeight = 0.5m };
        subjectRepo.ExistsAsync(cmd.SubjectId, Arg.Any<CancellationToken>()).Returns(true);
        skillRepo.ExistsAsync(cmd.SkillId, Arg.Any<CancellationToken>()).Returns(false);

        var sut = new AddSubjectSkillMappingCommandHandler(mappingRepo, subjectRepo, skillRepo, logger);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}
