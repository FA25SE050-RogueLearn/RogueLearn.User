using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands;

public class DeleteRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<ILogger<DeleteRoleCommandHandler>> _mockLogger;
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleCommandHandlerTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockLogger = new Mock<ILogger<DeleteRoleCommandHandler>>();
        _handler = new DeleteRoleCommandHandler(
            _mockRoleRepository.Object,
            _mockUserRoleRepository.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRoleId_DeletesRoleSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "Test Role",
            Description = "Test Description"
        };
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(existingRole);
        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<UserRole>());

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockRoleRepository.Verify(x => x.DeleteAsync(existingRole.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentRoleId_ThrowsNotFoundException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Message.Should().Be($"Entity \"Role\" ({roleId}) was not found.");
        _mockRoleRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RoleAssignedToUsers_ThrowsBadRequestException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "Test Role",
            Description = "Test Description"
        };
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(existingRole);
        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<UserRole> { new UserRole { AuthUserId = Guid.NewGuid(), RoleId = roleId } });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Message.Should().Be($"Cannot delete role '{existingRole.Name}' because it is assigned to 1 user(s). Remove the role from all users first.");
        _mockRoleRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_LogsInformation()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "Test Role",
            Description = "Test Description"
        };
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(existingRole);
        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<UserRole>());

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains($"Role '{existingRole.Name}' with ID {roleId} deleted")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidRequest_PassesCancellationToken()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "Test Role",
            Description = "Test Description"
        };
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = new CancellationToken(true);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(existingRole);
        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken))
            .ReturnsAsync(new List<UserRole>());

        // Act
        await _handler.Handle(command, cancellationToken);

        // Assert
        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, cancellationToken), Times.Once);
        _mockUserRoleRepository.Verify(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken), Times.Once);
        _mockRoleRepository.Verify(x => x.DeleteAsync(existingRole.Id, cancellationToken), Times.Once);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database error");

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(expectedException);
    }

    [Fact]
    public async Task Handle_UserRoleRepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "Test Role",
            Description = "Test Description"
        };
        var command = new DeleteRoleCommand { Id = roleId };
        var cancellationToken = CancellationToken.None;
        var expectedException = new InvalidOperationException("Database error");

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, cancellationToken))
            .ReturnsAsync(existingRole);
        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, cancellationToken))
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, cancellationToken));

        exception.Should().Be(expectedException);
        _mockRoleRepository.Verify(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}