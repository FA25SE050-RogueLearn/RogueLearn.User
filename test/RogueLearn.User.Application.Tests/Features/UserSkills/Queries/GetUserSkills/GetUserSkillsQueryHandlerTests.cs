using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.UserSkills.Queries.GetUserSkills;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserSkills.Queries.GetUserSkills;

public class GetUserSkillsQueryHandlerTests
{
    [Fact]
    public async Task Handle_Maps_UserSkills_To_Response()
    {
        var userId = Guid.NewGuid();
        var repo = Substitute.For<IUserSkillRepository>();
        repo.GetSkillsByAuthIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new UserSkill { AuthUserId = userId, SkillId = Guid.NewGuid(), SkillName = "A", ExperiencePoints = 100, Level = 2, LastUpdatedAt = DateTimeOffset.UtcNow },
            new UserSkill { AuthUserId = userId, SkillId = Guid.NewGuid(), SkillName = "B", ExperiencePoints = 50, Level = 1, LastUpdatedAt = DateTimeOffset.UtcNow },
        });

        var sut = new GetUserSkillsQueryHandler(repo);
        var res = await sut.Handle(new GetUserSkillsQuery { AuthUserId = userId }, CancellationToken.None);
        res.Skills.Count.Should().Be(2);
        res.Skills.Select(s => s.SkillName).Should().Contain(new[] { "A", "B" });
        res.Skills.All(s => s.SkillId != Guid.Empty).Should().BeTrue();
    }
}