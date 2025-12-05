using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Application.Features.Guilds.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_CreatesGuildAndMaster()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreateGuildCommandHandler(guildRepo, memberRepo, userRoleRepo, roleRepo);
        var creatorId = System.Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        guildRepo.GetGuildsByCreatorAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<Guild>());

        var verifiedRole = new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = verifiedRole.Id } });

        var createdGuild = new Guild { Id = System.Guid.NewGuid(), Name = "G", MaxMembers = 20, CreatedBy = creatorId };
        guildRepo.AddAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>()).Returns(createdGuild);

        var gmRole = new Role { Id = System.Guid.NewGuid(), Name = "Guild Master" };
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(gmRole);
        userRoleRepo.GetRolesForUserAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        userRoleRepo.AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserRole>());
        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = creatorId,
            Name = "G",
            Description = "D",
            Privacy = "public",
            MaxMembers = 20
        };
        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.GuildId.Should().Be(createdGuild.Id);
        resp.Guild.Should().BeOfType<GuildDto>();
        await memberRepo.Received(1).AddAsync(Arg.Is<GuildMember>(gm => gm.Role == GuildRole.GuildMaster), Arg.Any<CancellationToken>());
        await userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingGuildMasterRole_Throws()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreateGuildCommandHandler(guildRepo, memberRepo, userRoleRepo, roleRepo);
        var creatorId = System.Guid.NewGuid();
        memberRepo.GetMembershipsByUserAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        guildRepo.GetGuildsByCreatorAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<Guild>());

        var verifiedRole = new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(creatorId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = verifiedRole.Id } });

        var createdGuild = new Guild { Id = System.Guid.NewGuid(), Name = "G", MaxMembers = 20, CreatedBy = creatorId };
        guildRepo.AddAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>()).Returns(createdGuild);

        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns((Role?)null);

        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = creatorId,
            Name = "G",
            Description = "D",
            Privacy = "public",
            MaxMembers = 20
        };
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}