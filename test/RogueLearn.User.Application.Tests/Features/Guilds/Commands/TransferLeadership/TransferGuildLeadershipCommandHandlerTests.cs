using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.TransferLeadership;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.TransferLeadership;

public class TransferGuildLeadershipCommandHandlerTests
{
    [Fact]
    public async Task Handle_TransfersLeadershipAndUpdatesMembers()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var guildId = Guid.NewGuid();
        var newMasterUserId = Guid.NewGuid();

        var currentMaster = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        var otherMaster = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        var target = new GuildMember { GuildId = guildId, AuthUserId = newMasterUserId, Role = GuildRole.Member, Status = MemberStatus.Active };
        var inactiveTarget = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.Member, Status = MemberStatus.Inactive };

        repo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>())
            .Returns(new[] { currentMaster, otherMaster, target, inactiveTarget });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Guild Master" });
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new TransferGuildLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        await sut.Handle(new TransferGuildLeadershipCommand(guildId, newMasterUserId), CancellationToken.None);

        target.Role.Should().Be(GuildRole.GuildMaster);
        await repo.Received(1).UpdateAsync(target, Arg.Any<CancellationToken>());

        currentMaster.Role.Should().Be(GuildRole.Member);
        otherMaster.Role.Should().Be(GuildRole.Member);
        await repo.Received(2).UpdateAsync(Arg.Is<GuildMember>(m => m.Role == GuildRole.Member), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ThrowsWhenNoGuildMasterExists()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var guildId = Guid.NewGuid();
        var newMasterUserId = Guid.NewGuid();
        var activeMember = new GuildMember { GuildId = guildId, AuthUserId = newMasterUserId, Role = GuildRole.Member, Status = MemberStatus.Active };
        repo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>())
            .Returns(new[] { activeMember });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Guild Master" });
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new TransferGuildLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        var act = () => sut.Handle(new TransferGuildLeadershipCommand(guildId, newMasterUserId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task Handle_ThrowsWhenTargetNotFoundOrInactive()
    {
        var repo = Substitute.For<IGuildMemberRepository>();
        var guildId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();
        var master = new GuildMember { GuildId = guildId, AuthUserId = Guid.NewGuid(), Role = GuildRole.GuildMaster, Status = MemberStatus.Active };
        var inactiveTarget = new GuildMember { GuildId = guildId, AuthUserId = toUserId, Role = GuildRole.Member, Status = MemberStatus.Inactive };
        repo.GetMembersByGuildAsync(guildId, Arg.Any<CancellationToken>())
            .Returns(new[] { master, inactiveTarget });

        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Guild Master" });
        var notification = Substitute.For<IGuildNotificationService>();
        var sut = new TransferGuildLeadershipCommandHandler(repo, userRoleRepo, roleRepo, notification);
        var act = () => sut.Handle(new TransferGuildLeadershipCommand(guildId, toUserId), CancellationToken.None);
        await act.Should().ThrowAsync<RogueLearn.User.Application.Exceptions.NotFoundException>();
    }

    [Fact]
    public void Command_CarriesValues()
    {
        var guildId = Guid.NewGuid();
        var toUserId = Guid.NewGuid();
        var cmd = new TransferGuildLeadershipCommand(guildId, toUserId);
        cmd.GuildId.Should().Be(guildId);
        cmd.ToUserId.Should().Be(toUserId);
    }
}