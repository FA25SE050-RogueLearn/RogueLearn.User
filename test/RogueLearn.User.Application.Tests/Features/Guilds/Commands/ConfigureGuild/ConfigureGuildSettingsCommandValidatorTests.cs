using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.ConfigureGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Xunit;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.ConfigureGuild;

public class ConfigureGuildSettingsCommandValidatorTests
{
    [Fact]
    public async System.Threading.Tasks.Task ValidConfig_Passes()
    {
        var guildRepo = Substitute.For<IGuildRepository>();
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var validator = new ConfigureGuildSettingsCommandValidator(guildRepo, roleRepo, userRoleRepo);

        var guildId = System.Guid.NewGuid();
        var actorId = System.Guid.NewGuid();
        guildRepo.GetByIdAsync(guildId, Arg.Any<CancellationToken>()).Returns(new Guild { Id = guildId, CurrentMemberCount = 10 });
        var verifiedRole = new RogueLearn.User.Domain.Entities.Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(actorId, Arg.Any<CancellationToken>()).Returns(new List<RogueLearn.User.Domain.Entities.UserRole> { new() { RoleId = verifiedRole.Id } });

        var cmd = new ConfigureGuildSettingsCommand(guildId, actorId, "New Name", "Desc", "public", 20);
        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeTrue();
    }
}