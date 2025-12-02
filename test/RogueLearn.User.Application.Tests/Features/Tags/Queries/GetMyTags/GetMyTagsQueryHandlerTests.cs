using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RogueLearn.User.Application.Features.Tags.Queries.GetMyTags;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Tags.Queries.GetMyTags;

public class GetMyTagsQueryHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_ReturnsFiltered(Guid authUserId)
    {
        var repo = Substitute.For<ITagRepository>();
        var logger = Substitute.For<ILogger<GetMyTagsQueryHandler>>();
        var sut = new GetMyTagsQueryHandler(repo, logger);

        var tags = new List<Tag>
        {
            new() { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "Alpha" },
            new() { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "Beta" },
            new() { Id = Guid.NewGuid(), AuthUserId = authUserId, Name = "Gamma" }
        };
        repo.FindAsync(Arg.Any<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), Arg.Any<CancellationToken>()).Returns(tags);

        var result = await sut.Handle(new GetMyTagsQuery(authUserId, "am"), CancellationToken.None);

        result.Tags.Count.Should().Be(1);
        result.Tags.Select(t => t.Name).Should().Contain(new[] { "Gamma" });
    }
}