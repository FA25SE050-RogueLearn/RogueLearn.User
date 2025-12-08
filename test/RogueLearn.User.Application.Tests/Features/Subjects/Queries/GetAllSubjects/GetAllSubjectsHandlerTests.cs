using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Subjects.Queries.GetAllSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Subjects.Queries.GetAllSubjects;

public class GetAllSubjectsHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMappedDtos_WithPagination()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetAllSubjectsHandler>>();
        var sut = new GetAllSubjectsHandler(repo, mapper, logger);

        var subjects = new List<Subject>
        {
            new() { Id = Guid.NewGuid(), SubjectCode = "CS101", SubjectName = "Intro", Credits = 3 },
            new() { Id = Guid.NewGuid(), SubjectCode = "CS102", SubjectName = "Data", Credits = 4 }
        };
        repo.GetPagedSubjectsAsync(null, 2, 2, Arg.Any<CancellationToken>()).Returns((subjects, 5));

        var mapped = new List<SubjectDto>
        {
            new() { Id = subjects[0].Id, SubjectCode = subjects[0].SubjectCode, SubjectName = subjects[0].SubjectName, Credits = subjects[0].Credits },
            new() { Id = subjects[1].Id, SubjectCode = subjects[1].SubjectCode, SubjectName = subjects[1].SubjectName, Credits = subjects[1].Credits }
        };
        mapper.Map<List<SubjectDto>>(subjects).Returns(mapped);

        var res = await sut.Handle(new GetAllSubjectsQuery { Page = 2, PageSize = 2 }, CancellationToken.None);
        res.Items.Should().HaveCount(2);
        res.Page.Should().Be(2);
        res.PageSize.Should().Be(2);
        res.TotalCount.Should().Be(5);
        res.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task Handle_DtosContainDescriptionAndAudit()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetAllSubjectsHandler>>();
        var sut = new GetAllSubjectsHandler(repo, mapper, logger);

        var subjects = new List<Subject>
        {
            new() { Id = Guid.NewGuid(), SubjectCode = "CS301", SubjectName = "Subj", Credits = 3, Description = "d", CreatedAt = DateTimeOffset.UtcNow.AddDays(-2), UpdatedAt = DateTimeOffset.UtcNow.AddDays(-1) }
        };
        repo.GetPagedSubjectsAsync(null, 1, 1, Arg.Any<CancellationToken>()).Returns((subjects, 1));

        var mapped = new List<SubjectDto>
        {
            new() { Id = subjects[0].Id, SubjectCode = subjects[0].SubjectCode, SubjectName = subjects[0].SubjectName, Credits = subjects[0].Credits, Description = subjects[0].Description, CreatedAt = subjects[0].CreatedAt, UpdatedAt = subjects[0].UpdatedAt }
        };
        mapper.Map<List<SubjectDto>>(subjects).Returns(mapped);

        var res = await sut.Handle(new GetAllSubjectsQuery { Page = 1, PageSize = 1 }, CancellationToken.None);
        res.Items[0].Description.Should().Be("d");
        res.Items[0].CreatedAt.Should().Be(mapped[0].CreatedAt);
        res.Items[0].UpdatedAt.Should().Be(mapped[0].UpdatedAt);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        var repo = Substitute.For<ISubjectRepository>();
        var mapper = Substitute.For<IMapper>();
        var logger = Substitute.For<ILogger<GetAllSubjectsHandler>>();
        var sut = new GetAllSubjectsHandler(repo, mapper, logger);
        repo.GetPagedSubjectsAsync("search", 1, 10, Arg.Any<CancellationToken>()).Returns((new List<Subject>(), 0));
        mapper.Map<List<SubjectDto>>(Arg.Any<IEnumerable<Subject>>()).Returns((List<SubjectDto>)null!);
        var res = await sut.Handle(new GetAllSubjectsQuery { Page = 1, PageSize = 10, Search = "search" }, CancellationToken.None);
        res.Items.Should().NotBeNull();
        res.Items.Should().BeEmpty();
        res.TotalCount.Should().Be(0);
        res.TotalPages.Should().Be(0);
    }
}
