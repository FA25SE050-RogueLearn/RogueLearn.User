using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.GuildPosts.Commands;

public class CreateGuildPostCommandHandlerTests
{
    [Fact]
    public async Task CreateGuildPost_Should_Create_When_MemberActive()
    {
        var postRepo = new Mock<IGuildPostRepository>();
        var memberRepo = new Mock<IGuildMemberRepository>();
        var guildRepo = new Mock<IGuildRepository>();

        var guildId = Guid.NewGuid();
        guildRepo.Setup(r => r.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId });

        memberRepo.Setup(r => r.GetMemberAsync(guildId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId });

        postRepo.Setup(r => r.AddAsync(It.IsAny<GuildPost>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildPost p, CancellationToken _) => p);

        var handler = new CreateGuildPostCommandHandler(postRepo.Object, memberRepo.Object, guildRepo.Object);

        var cmd = new CreateGuildPostCommand(guildId, Guid.NewGuid(), new CreateGuildPostRequest
        {
            Title = "Test",
            Content = "Body",
            Tags = new[] { "general" },
            Attachments = new Dictionary<string, object>()
        });

        var resp = await handler.Handle(cmd, CancellationToken.None);

        Assert.NotNull(resp);
        Assert.NotEqual(Guid.Empty, resp.PostId);
    }
}