using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Subjects.Commands.CreateSubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Commands.CreateSubject;

public class CreateSubjectHandlerTests
{
    [Fact]
    public async Task Handle_Success_ReturnsResponse()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<CreateSubjectHandler>>();
        var sut = new CreateSubjectHandler(repo, mapper, logger);

        var cmd = new CreateSubjectCommand { SubjectCode = "CS101", SubjectName = "Intro", Credits = 3, Description = "desc" };
        var created = new Subject { Id = System.Guid.NewGuid(), SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description };
        repo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(created);
        mapper.Map<CreateSubjectResponse>(created).Returns(new CreateSubjectResponse { Id = created.Id, SubjectCode = created.SubjectCode, SubjectName = created.SubjectName, Credits = created.Credits });

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Id.Should().Be(created.Id);
        resp.SubjectCode.Should().Be(cmd.SubjectCode);
        await repo.Received(1).AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResponseContainsAuditAndDescription()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<CreateSubjectHandler>>();
        var sut = new CreateSubjectHandler(repo, mapper, logger);

        var cmd = new CreateSubjectCommand { SubjectCode = "CS103", SubjectName = "Systems", Credits = 3, Description = "desc" };
        var created = new Subject { Id = System.Guid.NewGuid(), SubjectCode = cmd.SubjectCode, SubjectName = cmd.SubjectName, Credits = cmd.Credits, Description = cmd.Description, CreatedAt = DateTimeOffset.UtcNow, UpdatedAt = DateTimeOffset.UtcNow };
        repo.AddAsync(Arg.Any<Subject>(), Arg.Any<CancellationToken>()).Returns(created);
        var expected = new CreateSubjectResponse { Id = created.Id, SubjectCode = created.SubjectCode, SubjectName = created.SubjectName, Credits = created.Credits, Description = created.Description, CreatedAt = created.CreatedAt, UpdatedAt = created.UpdatedAt };
        mapper.Map<CreateSubjectResponse>(created).Returns(expected);

        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.Description.Should().Be("desc");
        resp.CreatedAt.Should().Be(expected.CreatedAt);
        resp.UpdatedAt.Should().Be(expected.UpdatedAt);
    }
}
