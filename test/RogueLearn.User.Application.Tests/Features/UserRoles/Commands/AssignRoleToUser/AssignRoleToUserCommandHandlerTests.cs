using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.AssignRoleToUser;

public class AssignRoleToUserCommandHandlerTests
{
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<ILogger<AssignRoleToUserCommandHandler>> _mockLogger;
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserCommandHandlerTests()
    {
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockUserProfileRepository = new Mock<IUserProfileRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockLogger = new Mock<ILogger<AssignRoleToUserCommandHandler>>();

        _handler = new AssignRoleToUserCommandHandler(
            _mockUserRoleRepository.Object,
            _mockUserProfileRepository.Object,
            _mockRoleRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldAssignRoleSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var command = new AssignRoleToUserCommand
        {
            AuthUserId = authUserId,
            RoleId = roleId
        };

        var user = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authUserId };
        var role = new Role { Id = roleId, Name = "TestRole" };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>());

        _mockUserRoleRepository.Setup(x => x.AddAsync(It.IsAny<UserRole>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(It.IsAny<UserRole>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRoleRepository.Verify(
            x => x.AddAsync(It.Is<UserRole>(ur => ur.AuthUserId == authUserId && ur.RoleId == roleId), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new AssignRoleToUserCommand
        {
            AuthUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(command.AuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("User");
        exception.Message.Should().Contain(command.AuthUserId.ToString());
    }

    [Fact]
    public async Task Handle_RoleNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var command = new AssignRoleToUserCommand
        {
            AuthUserId = authUserId,
            RoleId = roleId
        };

        var user = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authUserId };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Role");
        exception.Message.Should().Contain(roleId.ToString());
    }

    [Fact]
    public async Task Handle_UserAlreadyHasRole_ShouldThrowBadRequestException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var command = new AssignRoleToUserCommand
        {
            AuthUserId = authUserId,
            RoleId = roleId
        };

        var user = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authUserId };
        var role = new Role { Id = roleId, Name = "TestRole" };
        var existingUserRole = new UserRole { AuthUserId = authUserId, RoleId = roleId };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole> { existingUserRole });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("already has the role");
        exception.Message.Should().Contain("TestRole");
    }
}