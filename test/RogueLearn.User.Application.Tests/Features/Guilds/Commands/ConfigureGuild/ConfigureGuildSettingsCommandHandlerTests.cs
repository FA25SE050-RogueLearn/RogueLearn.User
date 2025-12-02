using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandHandlerTests
{
    [Theory]
    [AutoData]
    public async Task Handle_NotFoundGuild_Throws(ConfigureGuildSettingsCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_NotVerifiedAndLocked_Throws(ConfigureGuildSettingsCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guild = new Guild { Id = cmd.GuildId, CurrentMemberCount = 50 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        cmd = new ConfigureGuildSettingsCommand(cmd.GuildId, cmd.ActorAuthUserId, "Name", "Desc", "public", 51);
        await Assert.ThrowsAsync<UnprocessableEntityException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_MaxMembersTooSmall_Throws(ConfigureGuildSettingsCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guild = new Guild { Id = cmd.GuildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        cmd = new ConfigureGuildSettingsCommand(cmd.GuildId, cmd.ActorAuthUserId, "Name", "Desc", "public", 5);
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Theory]
    [AutoData]
    public async Task Handle_Success_Updates(ConfigureGuildSettingsCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guild = new Guild { Id = cmd.GuildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(guild);
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = System.Guid.NewGuid() } });
        cmd = new ConfigureGuildSettingsCommand(cmd.GuildId, cmd.ActorAuthUserId, "Name", "Desc", "public", 20);
        await sut.Handle(cmd, CancellationToken.None);
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.MaxMembers == 20 && g.IsPublic == true), Arg.Any<CancellationToken>());
    }
}