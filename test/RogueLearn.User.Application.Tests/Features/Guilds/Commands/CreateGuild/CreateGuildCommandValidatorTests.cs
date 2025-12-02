using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using RogueLearn.User.Application.Features.Guilds.Commands.CreateGuild;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Guilds.Commands.CreateGuild;

public class CreateGuildCommandValidatorTests
{
    [Fact]
    public async Task Valid_NonVerified_WithinCap_Passes()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var validator = new CreateGuildCommandValidator(roleRepo, userRoleRepo);

        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = System.Guid.NewGuid(),
            Name = "Guild",
            Description = "Desc",
            Privacy = "public",
            MaxMembers = 20
        };

        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_Privacy_Fails()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var validator = new CreateGuildCommandValidator(roleRepo, userRoleRepo);

        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = System.Guid.NewGuid(),
            Name = "Guild",
            Description = "Desc",
            Privacy = "friends_only",
            MaxMembers = 10
        };

        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuildCommand.Privacy));
    }

    [Fact]
    public async Task Missing_Required_Fields_Fail()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var validator = new CreateGuildCommandValidator(roleRepo, userRoleRepo);

        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = default,
            Name = string.Empty,
            Description = "Desc",
            Privacy = "public",
            MaxMembers = 10
        };

        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuildCommand.CreatorAuthUserId));
        res.Errors.Should().Contain(e => e.PropertyName == nameof(CreateGuildCommand.Name));
    }

    [Fact]
    public async Task NonVerified_AboveCap_Fails()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns((Role?)null);
        userRoleRepo.GetRolesForUserAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole>());
        var validator = new CreateGuildCommandValidator(roleRepo, userRoleRepo);

        var cmd = new CreateGuildCommand
        {
            CreatorAuthUserId = System.Guid.NewGuid(),
            Name = "Guild",
            Description = "Desc",
            Privacy = "public",
            MaxMembers = 51
        };

        var res = await validator.ValidateAsync(cmd, CancellationToken.None);
        res.IsValid.Should().BeFalse();
        res.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task VerifiedLecturer_UpTo100_Passes_Above100_Fails()
    {
        var roleRepo = Substitute.For<IRoleRepository>();
        var userRoleRepo = Substitute.For<IUserRoleRepository>();
        var verifiedRole = new Role { Id = System.Guid.NewGuid(), Name = "Verified Lecturer" };
        roleRepo.GetByNameAsync("Verified Lecturer", Arg.Any<CancellationToken>()).Returns(verifiedRole);
        userRoleRepo.GetRolesForUserAsync(Arg.Any<System.Guid>(), Arg.Any<CancellationToken>()).Returns(new List<UserRole> { new() { RoleId = verifiedRole.Id } });
        var validator = new CreateGuildCommandValidator(roleRepo, userRoleRepo);

        var ok = new CreateGuildCommand
        {
            CreatorAuthUserId = System.Guid.NewGuid(),
            Name = "Guild",
            Description = "Desc",
            Privacy = "invite_only",
            MaxMembers = 100
        };
        var okRes = await validator.ValidateAsync(ok, CancellationToken.None);
        okRes.IsValid.Should().BeTrue();

        var bad = ok with { MaxMembers = 101 };
        var badRes = await validator.ValidateAsync(bad, CancellationToken.None);
        badRes.IsValid.Should().BeFalse();
    }
}