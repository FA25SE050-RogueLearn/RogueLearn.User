using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using RogueLearn.User.Application.Features.Parties.Commands.CreateParty;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Tests.Features.Parties.Commands.CreateParty;

public class CreatePartyCommandHandlerTests
{
    private readonly Mock<IPartyRepository> _partyRepository = new();
    private readonly Mock<IPartyMemberRepository> _partyMemberRepository = new();
    private readonly Mock<IUserRoleRepository> _userRoleRepository = new();
    private readonly Mock<IRoleRepository> _roleRepository = new();

    private readonly CreatePartyCommandHandler _handler;

    public CreatePartyCommandHandlerTests()
    {
        _handler = new CreatePartyCommandHandler(
            _partyRepository.Object,
            _partyMemberRepository.Object,
            _userRoleRepository.Object,
            _roleRepository.Object
        );
    }

    [Fact]
    public async Task Handle_ShouldCreateParty_AddLeaderMember_AssignLeaderRole()
    {
        // Arrange
        var creatorId = Guid.NewGuid();
        var leaderRoleId = Guid.NewGuid();
        var cmd = new CreatePartyCommand
        {
            CreatorAuthUserId = creatorId,
            Name = "Test Party",
            IsPublic = true,
            MaxMembers = 6
        };

        _partyRepository
            .Setup(r => r.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Party p, CancellationToken _) => { p.Id = Guid.NewGuid(); return p; });

        _partyMemberRepository
            .Setup(r => r.AddAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember m, CancellationToken _) => m);

        _roleRepository
            .Setup(r => r.GetByNameAsync("Party Leader", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = leaderRoleId, Name = "Party Leader" });

        _userRoleRepository
            .Setup(r => r.GetRolesForUserAsync(creatorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>());

        _userRoleRepository
            .Setup(r => r.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRole ur, CancellationToken _) => ur);

        // Act
        var result = await _handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PartyId.Should().NotBe(Guid.Empty);
        result.RoleGranted.Should().Be("PartyLeader");

        _partyRepository.Verify(r => r.AddAsync(It.IsAny<Party>(), It.IsAny<CancellationToken>()), Times.Once);
        _partyMemberRepository.Verify(r => r.AddAsync(It.Is<PartyMember>(m => m.Role == PartyRole.Leader && m.AuthUserId == creatorId), It.IsAny<CancellationToken>()), Times.Once);
        _userRoleRepository.Verify(r => r.AddAsync(It.Is<UserRole>(ur => ur.AuthUserId == creatorId && ur.RoleId == leaderRoleId), It.IsAny<CancellationToken>()), Times.Once);
    }
}