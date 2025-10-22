using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.Roles.Queries.GetAllRoles;

public class GetAllRolesQueryHandlerTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetAllRolesQueryHandler>> _mockLogger;
    private readonly GetAllRolesQueryHandler _handler;

    public GetAllRolesQueryHandlerTests()
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetAllRolesQueryHandler>>();
        _handler = new GetAllRolesQueryHandler(_mockRoleRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithExistingRoles_ShouldReturnAllRoles()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Admin",
                Description = "Administrator role",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "User",
                Description = "Regular user role",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "Moderator",
                Description = null,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockRoleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        var expectedRoleDtos = new List<RoleDto>
        {
            new RoleDto
            {
                Id = roles[0].Id,
                Name = "Admin",
                Description = "Administrator role",
                CreatedAt = roles[0].CreatedAt
            },
            new RoleDto
            {
                Id = roles[1].Id,
                Name = "User",
                Description = "Regular user role",
                CreatedAt = roles[1].CreatedAt
            },
            new RoleDto
            {
                Id = roles[2].Id,
                Name = "Moderator",
                Description = null,
                CreatedAt = roles[2].CreatedAt
            }
        };

        _mockMapper.Setup(x => x.Map<List<RoleDto>>(roles))
            .Returns(expectedRoleDtos);

        var query = new GetAllRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(3);
        
        result.Roles[0].Id.Should().Be(roles[0].Id);
        result.Roles[0].Name.Should().Be("Admin");
        result.Roles[0].Description.Should().Be("Administrator role");
        result.Roles[0].CreatedAt.Should().Be(roles[0].CreatedAt);

        result.Roles[1].Id.Should().Be(roles[1].Id);
        result.Roles[1].Name.Should().Be("User");
        result.Roles[1].Description.Should().Be("Regular user role");
        result.Roles[1].CreatedAt.Should().Be(roles[1].CreatedAt);

        result.Roles[2].Id.Should().Be(roles[2].Id);
        result.Roles[2].Name.Should().Be("Moderator");
        result.Roles[2].Description.Should().BeNull();
        result.Roles[2].CreatedAt.Should().Be(roles[2].CreatedAt);

        _mockRoleRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<List<RoleDto>>(roles), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoRoles_ShouldReturnEmptyList()
    {
        // Arrange
        var emptyRoles = new List<Role>();
        var emptyRoleDtos = new List<RoleDto>();

        _mockRoleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyRoles);

        _mockMapper.Setup(x => x.Map<List<RoleDto>>(emptyRoles))
            .Returns(emptyRoleDtos);

        var query = new GetAllRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().BeEmpty();

        _mockRoleRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockMapper.Verify(x => x.Map<List<RoleDto>>(emptyRoles), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseAutoMapper()
    {
        // Arrange
        var roles = new List<Role>
        {
            new Role
            {
                Id = Guid.NewGuid(),
                Name = "TestRole",
                Description = "Test description",
                CreatedAt = DateTime.UtcNow
            }
        };

        var expectedRoleDtos = new List<RoleDto>
        {
            new RoleDto
            {
                Id = roles[0].Id,
                Name = "TestRole",
                Description = "Test description",
                CreatedAt = roles[0].CreatedAt
            }
        };

        _mockRoleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        _mockMapper.Setup(x => x.Map<List<RoleDto>>(roles))
            .Returns(expectedRoleDtos);

        var query = new GetAllRolesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Roles.Should().HaveCount(1);

        // Verify AutoMapper was called
        _mockMapper.Verify(x => x.Map<List<RoleDto>>(roles), Times.Once);
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassTokenToRepository()
    {
        // Arrange
        var roles = new List<Role>();
        var emptyRoleDtos = new List<RoleDto>();
        var cancellationToken = new CancellationToken();

        _mockRoleRepository.Setup(x => x.GetAllAsync(cancellationToken))
            .ReturnsAsync(roles);

        _mockMapper.Setup(x => x.Map<List<RoleDto>>(roles))
            .Returns(emptyRoleDtos);

        var query = new GetAllRolesQuery();

        // Act
        await _handler.Handle(query, cancellationToken);

        // Assert
        _mockRoleRepository.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
        _mockMapper.Verify(x => x.Map<List<RoleDto>>(roles), Times.Once);
    }
}