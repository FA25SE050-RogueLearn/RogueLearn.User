using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_NotFoundGuild_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guildId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "Name", "Desc", "public", 10);
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }



    [Fact]
    public async Task Handle_MaxMembersTooSmall_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guildId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "Name", "Desc", "public", 5);
        var guild = new Guild { Id = guildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(actorId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        await Assert.ThrowsAsync<BadRequestException>(() => sut.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_Success_Updates()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guildId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "Name", "Desc", "public", 20);
        var guild = new Guild { Id = guildId, CurrentMemberCount = 10 };
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(guild);
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(actorId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = System.Guid.NewGuid() } });
        await sut.Handle(cmd, CancellationToken.None);
        await guildRepo.Received(1).UpdateAsync(Arg.Is<Guild>(g => g.MaxMembers == 20 && g.IsPublic == true), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaxMembersExceedsRoleLimit_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, CurrentMemberCount = 10 });

        var verifiedRole = new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(actorId, Arg.Any<CancellationToken>()).Returns(Array.Empty<UserRole>());

        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "G", "D", "private", 51);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }

    [Fact]
    public async Task Handle_MaxMembersNotGreaterThanCurrent_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var sut = new ConfigureGuildSettingsCommandHandler(guildRepo, roleRepo, userRoleRepo);

        var guildId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, CurrentMemberCount = 20 });

        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "G", "D", "public", 20);

        var act = () => sut.Handle(cmd, CancellationToken.None);
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.BadRequestException>(act);
    }
}
