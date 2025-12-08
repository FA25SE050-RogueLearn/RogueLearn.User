using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.ClassSpecialization.Queries.GetSpecializationSubjects;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.ClassSpecialization.Queries.GetSpecializationSubjects;

public class GetSpecializationSubjectsQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetSpecializationSubjectsQueryHandler(repo, mapper);

        var query = new GetSpecializationSubjectsQuery { ClassId = System.Guid.NewGuid() };
        var items = new List<ClassSpecializationSubject> { new() { Id = System.Guid.NewGuid(), ClassId = query.ClassId, SubjectId = System.Guid.NewGuid(), Semester = 1, PlaceholderSubjectCode = "PH" } };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>()).Returns(items);
        mapper.Map<List<SpecializationSubjectDto>>(items).Returns(new List<SpecializationSubjectDto> { new() { Id = items[0].Id, ClassId = items[0].ClassId, SubjectId = items[0].SubjectId, Semester = items[0].Semester, PlaceholderSubjectCode = items[0].PlaceholderSubjectCode } });

        var result = await sut.Handle(query, CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Semester.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        var repo = Substitute.For<IClassSpecializationSubjectRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var sut = new GetSpecializationSubjectsQueryHandler(repo, mapper);

        var query = new GetSpecializationSubjectsQuery { ClassId = System.Guid.NewGuid() };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<ClassSpecializationSubject, bool>>>(), Arg.Any<CancellationToken>())
            .Returns(new List<ClassSpecializationSubject>());
        mapper.Map<List<SpecializationSubjectDto>>(Arg.Any<List<ClassSpecializationSubject>>()).Returns(new List<SpecializationSubjectDto>());

        var result = await sut.Handle(query, CancellationToken.None);
        result.Should().BeEmpty();
    }
}
