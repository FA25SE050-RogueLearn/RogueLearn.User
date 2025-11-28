using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Commands.RemoveRoleFromUser;

public class RemoveRoleFromUserCommandHandlerTests
{
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<ILogger<RemoveRoleFromUserCommandHandler>> _mockLogger;
    private readonly RemoveRoleFromUserCommandHandler _handler;

    public RemoveRoleFromUserCommandHandlerTests()
    {
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockUserProfileRepository = new Mock<IUserProfileRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockLogger = new Mock<ILogger<RemoveRoleFromUserCommandHandler>>();

        _handler = new RemoveRoleFromUserCommandHandler(
            _mockUserRoleRepository.Object,
            _mockUserProfileRepository.Object,
            _mockRoleRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldRemoveRoleSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();

        var command = new RemoveRoleFromUserCommand
        {
            AuthUserId = authUserId,
            RoleId = roleId
        };

        var user = new UserProfile { Id = Guid.NewGuid(), AuthUserId = authUserId };
        var role = new Role { Id = roleId, Name = "TestRole" };
        var userRole = new UserRole { Id = userRoleId, AuthUserId = authUserId, RoleId = roleId };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole> { userRole });

        _mockUserRoleRepository.Setup(x => x.DeleteAsync(userRoleId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mockUserRoleRepository.Verify(x => x.DeleteAsync(userRoleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var command = new RemoveRoleFromUserCommand
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
        
        var command = new RemoveRoleFromUserCommand
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
    public async Task Handle_UserDoesNotHaveRole_ShouldBeIdempotent_NoDelete()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var command = new RemoveRoleFromUserCommand
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

        await _handler.Handle(command, CancellationToken.None);
        _mockUserRoleRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}