using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.UpdateRole;

public class UpdateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<UpdateRoleCommandHandler>> _mockLogger;
    private readonly UpdateRoleCommandHandler _handler;

    public UpdateRoleCommandHandlerTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<UpdateRoleCommandHandler>>();
        _handler = new UpdateRoleCommandHandler(_mockRoleRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldUpdateRoleSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        var existingRole = new Role
        {
            Id = roleId,
            Name = "OldRole",
            Description = "Old description",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var updatedRole = new Role
        {
            Id = roleId,
            Name = command.Name,
            Description = command.Description,
            CreatedAt = existingRole.CreatedAt
        };

        var expectedResponse = new UpdateRoleResponse
        {
            Id = updatedRole.Id,
            Name = updatedRole.Name,
            Description = updatedRole.Description,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockRoleRepository.Setup(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRole);

        _mockMapper.Setup(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.Is<Role>(r => 
            r.Id == roleId &&
            r.Name == command.Name && 
            r.Description == command.Description), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldUpdateRoleSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "UpdatedRole",
            Description = null
        };

        var existingRole = new Role
        {
            Id = roleId,
            Name = "OldRole",
            Description = "Old description",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var updatedRole = new Role
        {
            Id = roleId,
            Name = command.Name,
            Description = null,
            CreatedAt = existingRole.CreatedAt
        };

        var expectedResponse = new UpdateRoleResponse
        {
            Id = updatedRole.Id,
            Name = updatedRole.Name,
            Description = null,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockRoleRepository.Setup(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRole);

        _mockMapper.Setup(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Description.Should().BeNull();
        _mockMapper.Verify(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentRole_ShouldThrowNotFoundException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "UpdatedRole",
            Description = "Updated description"
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => 
            _handler.Handle(command, CancellationToken.None));

        exception.Message.Should().Contain("Role");
        exception.Message.Should().Contain(roleId.ToString());

        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleRepository.Verify(x => x.GetByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldThrowBadRequestException()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "ExistingRole",
            Description = "Updated description"
        };

        var existingRole = new Role
        {
            Id = roleId,
            Name = "OldRole",
            Description = "Old description"
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        var updatedRole = new Role
        {
            Id = roleId,
            Name = command.Name,
            Description = command.Description,
            CreatedAt = existingRole.CreatedAt
        };

        var expectedResponse = new UpdateRoleResponse
        {
            Id = updatedRole.Id,
            Name = updatedRole.Name,
            Description = updatedRole.Description,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRoleRepository.Setup(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRole);

        _mockMapper.Setup(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithSameRoleNameUpdate_ShouldUpdateSuccessfully()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "SameRole",
            Description = "Updated description"
        };

        var existingRole = new Role
        {
            Id = roleId,
            Name = "SameRole",
            Description = "Old description",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        var updatedRole = new Role
        {
            Id = roleId,
            Name = command.Name,
            Description = command.Description,
            CreatedAt = existingRole.CreatedAt
        };

        var expectedResponse = new UpdateRoleResponse
        {
            Id = updatedRole.Id,
            Name = updatedRole.Name,
            Description = updatedRole.Description,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockRoleRepository.Setup(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRole);

        _mockMapper.Setup(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(roleId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _mockRoleRepository.Verify(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRoleRepository.Verify(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<UpdateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }
}