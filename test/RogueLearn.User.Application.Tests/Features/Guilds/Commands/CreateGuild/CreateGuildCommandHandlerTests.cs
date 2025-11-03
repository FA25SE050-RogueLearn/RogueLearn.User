using System.Threading;
using System.Threading.Tasks;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandHandlerTests
{
    [Fact]
    public async Task CreateGuild_Throws_WhenUserAlreadyMemberOfGuild()
    {
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var userRoleRepo = new Mock<IUserRoleRepository>(MockBehavior.Strict);
        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        memberRepo.Setup(r => r.GetMembershipsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new GuildMember { GuildId = Guid.NewGuid(), AuthUserId = userId, Status = MemberStatus.Active }
            });

        var handler = new CreateGuildCommandHandler(guildRepo.Object, memberRepo.Object, userRoleRepo.Object, roleRepo.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateGuildCommand
        {
            CreatorAuthUserId = userId,
            Name = "Test Guild"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateGuild_Throws_WhenUserAlreadyCreatedAGuild()
    {
        var guildRepo = new Mock<IGuildRepository>(MockBehavior.Strict);
        var memberRepo = new Mock<IGuildMemberRepository>(MockBehavior.Strict);
        var userRoleRepo = new Mock<IUserRoleRepository>(MockBehavior.Strict);
        var roleRepo = new Mock<IRoleRepository>(MockBehavior.Strict);

        var userId = Guid.NewGuid();
        memberRepo.Setup(r => r.GetMembershipsByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<GuildMember>());
        guildRepo.Setup(r => r.GetGuildsByCreatorAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Guild { Id = Guid.NewGuid(), CreatedBy = userId } });

        var handler = new CreateGuildCommandHandler(guildRepo.Object, memberRepo.Object, userRoleRepo.Object, roleRepo.Object);
        await Assert.ThrowsAsync<BadRequestException>(() => handler.Handle(new CreateGuildCommand
        {
            CreatorAuthUserId = userId,
            Name = "Another Guild"
        }, CancellationToken.None));
    }
}