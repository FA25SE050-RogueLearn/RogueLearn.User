using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Application.Features.Subjects.Queries.GetSubjectById;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Queries.GetSubjectById;

public class GetSubjectByIdHandlerTests
{
    [Fact]
    public async Task Handle_NotFound_ReturnsNull()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetSubjectByIdHandler>>();
        var sut = new GetSubjectByIdHandler(repo, mapper, logger);

        var id = Guid.NewGuid();
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((Subject?)null);

        var result = await sut.Handle(new GetSubjectByIdQuery { Id = id }, CancellationToken.None);
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_Found_ReturnsMappedDto()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetSubjectByIdHandler>>();
        var sut = new GetSubjectByIdHandler(repo, mapper, logger);

        var id = Guid.NewGuid();
        var subject = new Subject { Id = id, SubjectCode = "CS101", SubjectName = "Intro", Credits = 3 };
        repo.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(subject);
        mapper.Map<SubjectDto>(subject).Returns(new SubjectDto { Id = id, SubjectCode = "CS101", SubjectName = "Intro", Credits = 3 });

        var result = await sut.Handle(new GetSubjectByIdQuery { Id = id }, CancellationToken.None);
        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.SubjectCode.Should().Be("CS101");
    }
}