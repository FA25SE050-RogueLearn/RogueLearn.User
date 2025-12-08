using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandValidatorTests
{
    private static (ConfigureGuildSettingsCommandValidator v, IGuildRepository guildRepo, IRoleRepository roleRepo, IUserRoleRepository userRoleRepo) Create()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var v = new ConfigureGuildSettingsCommandValidator(guildRepo, roleRepo, userRoleRepo);
        return (v, guildRepo, roleRepo, userRoleRepo);
    }

    [Fact]
    public async Task Guild_Not_Found_Fails_MaxMembers_Rule()
    {
        var (v, guildRepo, roleRepo, userRoleRepo) = Create();
        var cmd = new ConfigureGuildSettingsCommand(Guid.NewGuid(), Guid.NewGuid(), "G", "D", "public", 10);
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns((Guild?)null);

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Exceeds_Cap_For_Non_Verified_Fails()
    {
        var (v, guildRepo, roleRepo, userRoleRepo) = Create();
        var cmd = new ConfigureGuildSettingsCommand(Guid.NewGuid(), Guid.NewGuid(), "G", "D", "public", 60);
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, CurrentMemberCount = 10 });
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole>());

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Invalid_Privacy_Fails()
    {
        var (v, guildRepo, roleRepo, userRoleRepo) = Create();
        var cmd = new ConfigureGuildSettingsCommand(Guid.NewGuid(), Guid.NewGuid(), "G", "D", "friends_only", 10);
        guildRepo.GetByIdAsync(cmd.GuildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = cmd.GuildId, CurrentMemberCount = 1 });
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(new Role { Id = Guid.NewGuid(), Name = "Verified Lecturer" });
        userRoleRepo.GetRolesForUserAsync(cmd.ActorAuthUserId, Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new UserRole { RoleId = Guid.NewGuid() } });

        var res = await v.ValidateAsync(cmd);
        res.IsValid.Should().BeFalse();
    }
}
