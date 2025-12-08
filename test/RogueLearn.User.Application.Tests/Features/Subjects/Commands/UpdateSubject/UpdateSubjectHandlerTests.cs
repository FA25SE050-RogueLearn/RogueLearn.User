using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Subjects.Commands.UpdateSubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.UpdateSubject;

public class UpdateSubjectHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_Throws()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateSubjectHandler>>();
        var sut = new UpdateSubjectHandler(repo, mapper, logger);
        var cmd = new UpdateSubjectCommand { Id = Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Intro", Credits = 3, Description = "desc" };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Subject?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateSubjectHandler>>();
        var sut = new UpdateSubjectHandler(repo, mapper, logger);
        var cmd = new UpdateSubjectCommand { Id = Guid.NewGuid(), SubjectCode = "CS201", SubjectName = "Algo", Credits = 4, Description = "desc2" };
        var subject = new Subject { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(subject);
        repo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        mapper.Map<UpdateSubjectResponse>(Arg.Any<Subject>()).Returns(new UpdateSubjectResponse { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Id.Should().Be(cmd.Id);
        resp.SubjectName.Should().Be(cmd.SubjectName);
        await repo.Received(1).UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResponseContainsAuditAndDescription()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateSubjectHandler>>();
        var sut = new UpdateSubjectHandler(repo, mapper, logger);

        var cmd = new UpdateSubjectCommand { Id = Guid.NewGuid(), SubjectCode = "CS204", SubjectName = "DB", Credits = 3, Description = "db desc" };
        var subject = new Subject { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1), UpdatedAt = DateTimeOffset.UtcNow };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(subject);
        repo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        var expected = new UpdateSubjectResponse { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description, CreatedAt = subject.CreatedAt, UpdatedAt = subject.UpdatedAt };
        mapper.Map<UpdateSubjectResponse>(Arg.Any<Subject>()).Returns(expected);

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Description.Should().Be("db desc");
        resp.CreatedAt.Should().Be(expected.CreatedAt);
        resp.UpdatedAt.Should().Be(expected.UpdatedAt);
    }

}
