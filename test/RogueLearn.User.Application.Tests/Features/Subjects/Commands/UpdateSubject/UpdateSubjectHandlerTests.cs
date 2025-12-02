using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_NotFound_Throws(UpdateSubjectCommand cmd)
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateSubjectHandler>>();
        var sut = new UpdateSubjectHandler(repo, mapper, logger);

        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns((Subject?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(UpdateSubjectCommand cmd)
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<UpdateSubjectHandler>>();
        var sut = new UpdateSubjectHandler(repo, mapper, logger);

        var subject = new Subject { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description };
        repo.GetByIdAsync(cmd.Id, Arg.Any<CancellationToken>()).Returns(subject);
        repo.UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<Subject>());
        mapper.Map<UpdateSubjectResponse>(Arg.Any<Subject>()).Returns(new UpdateSubjectResponse { Id = cmd.Id, SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Id.Should().Be(cmd.Id);
        resp.SubjectName.Should().Be(cmd.SubjectName);
        await repo.Received(1).UpdateAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>());
    }
}