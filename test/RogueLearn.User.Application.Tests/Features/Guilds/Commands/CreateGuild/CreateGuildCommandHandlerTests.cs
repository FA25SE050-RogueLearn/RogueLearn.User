using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture.Xunit2;
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
    [Theory]
    [AutoData]
    public async Task Handle_Success_CreatesGuildAndMaster(CreateGuildCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreateGuildCommandHandler(guildRepo, memberRepo, userRoleRepo, roleRepo);

        memberRepo.GetMembershipsByUserAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        guildRepo.GetGuildsByCreatorAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<Guild>());

        var verifiedRole = new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = verifiedRole.Id } });

        var createdGuild = new Guild { Id = System.Guid.NewGuid(), Name = cmd.Name, MaxMembers = cmd.MaxMembers, CreatedBy = cmd.CreatorAuthUserId };
        guildRepo.AddAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>()).Returns(createdGuild);

        var gmRole = new Role { Id = System.Guid.NewGuid(), Name = "Guild Master" };
        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns(gmRole);
        userRoleRepo.GetRolesForUserAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        userRoleRepo.AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>()).Returns(ci => ci.Arg<UserRole>());

        cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = cmd.CreatorAuthUserId,
            Name = cmd.Name,
            Description = cmd.Description,
            Privacy = "public",
            MaxMembers = 20
        };
        var resp = await sut.Handle(cmd, CancellationToken.None);
        resp.GuildId.Should().Be(createdGuild.Id);
        resp.Guild.Should().BeOfType<GuildDto>();
        await memberRepo.Received(1).AddAsync(Arg.Is<GuildMember>(gm => gm.Role == GuildRole.GuildMaster), Arg.Any<CancellationToken>());
        await userRoleRepo.Received(1).AddAsync(Arg.Any<UserRole>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [AutoData]
    public async Task Handle_MissingGuildMasterRole_Throws(CreateGuildCommand cmd)
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var memberRepo = Substitute.For<IGuildMemberRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var sut = new CreateGuildCommandHandler(guildRepo, memberRepo, userRoleRepo, roleRepo);

        memberRepo.GetMembershipsByUserAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<GuildMember>());
        guildRepo.GetGuildsByCreatorAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<Guild>());

        var verifiedRole = new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(cmd.CreatorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = verifiedRole.Id } });

        var createdGuild = new Guild { Id = System.Guid.NewGuid(), Name = cmd.Name, MaxMembers = 20, CreatedBy = cmd.CreatorAuthUserId };
        guildRepo.AddAsync(Arg.Any<Guild>(), Arg.Any<CancellationToken>()).Returns(createdGuild);

        roleRepo.GetByNameAsync("Guild Master", Arg.Any<CancellationToken>()).Returns((Role?)null);

        cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = cmd.CreatorAuthUserId,
            Name = cmd.Name,
            Description = cmd.Description,
            Privacy = "public",
            MaxMembers = 20
        };
        await Assert.ThrowsAsync<RogueLearn.User.Application.Exceptions.NotFoundException>(() => sut.Handle(cmd, CancellationToken.None));
    }
}