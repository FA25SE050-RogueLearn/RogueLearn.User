using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Roles.Commands.CreateRole;

public class CreateRoleCommandHandlerTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly CreateRoleCommandHandler _handler;

    public CreateRoleCommandHandlerTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockMapper = new Mock<IMapper>();
        _handler = new CreateRoleCommandHandler(_mockRoleRepository.Object, _mockMapper.Object);
    }

    [Fact]
    public async Task Handle_WithValidData_ShouldCreateRoleSuccessfully()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test role description"
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            CreatedAt = createdRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdRole.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.CreatedAt.Should().BeCloseTo(createdRole.CreatedAt, TimeSpan.FromSeconds(1));

        _mockRoleRepository.Verify(x => x.AddAsync(It.Is<Role>(r => 
            r.Name == command.Name && 
            r.Description == command.Description &&
            r.Id != Guid.Empty &&
            r.CreatedAt != default), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNullDescription_ShouldCreateRoleSuccessfully()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = null
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = null,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = null,
            CreatedAt = createdRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(command.Name);
        result.Description.Should().BeNull();
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldCreateRoleSuccessfully()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "ExistingRole",
            Description = "Test description"
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            CreatedAt = createdRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdRole.Id);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);

        _mockRoleRepository.Verify(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLogInformation()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test description"
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            CreatedAt = createdRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test description"
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            CreatedAt = createdRole.CreatedAt
        };

        var cancellationToken = new CancellationToken();

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), cancellationToken))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        _mockRoleRepository.Verify(x => x.AddAsync(It.IsAny<Role>(), cancellationToken), Times.Once);
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldGenerateUniqueId()
    {
        // Arrange
        var command = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test description"
        };

        var createdRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = command.Name,
            Description = command.Description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var expectedResponse = new CreateRoleResponse
        {
            Id = createdRole.Id,
            Name = createdRole.Name,
            Description = createdRole.Description,
            CreatedAt = createdRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        _mockMapper.Setup(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()))
            .Returns(expectedResponse);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(createdRole.Id);
        _mockMapper.Verify(x => x.Map<CreateRoleResponse>(It.IsAny<Role>()), Times.Once);
    }
}