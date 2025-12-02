using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Onboarding.Queries.GetAllClasses;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Onboarding.Queries.GetAllClasses;

public class GetAllClassesQueryHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsMapped()
    {
        var repo = Substitute.For<IClassRepository>();
        var mapper = Substitute.For<AutoMapper.IMapper>();
        var logger = Substitute.For<ILogger<GetAllClassesQueryHandler>>();
        var sut = new GetAllClassesQueryHandler(repo, mapper, logger);

        var classes = new List<Class> { new() { Id = System.Guid.NewGuid(), IsActive = true } };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<System.Func<Class, bool>>>(), Arg.Any<CancellationToken>()).Returns(classes);
        mapper.Map<List<ClassDto>>(classes).Returns(new List<ClassDto> { new() { Id = classes[0].Id } });

        var result = await sut.Handle(new GetAllClassesQuery(), CancellationToken.None);
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(classes[0].Id);
    }
}