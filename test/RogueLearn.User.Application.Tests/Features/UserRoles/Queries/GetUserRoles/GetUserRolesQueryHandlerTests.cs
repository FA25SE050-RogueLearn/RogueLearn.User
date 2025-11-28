using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Application.Features.UserRoles.Queries.GetUserRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Tests.Features.UserRoles.Queries.GetUserRoles;

public class GetUserRolesQueryHandlerTests
{
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<GetUserRolesQueryHandler>> _mockLogger;
    private readonly GetUserRolesQueryHandler _handler;

    public GetUserRolesQueryHandlerTests()
    {
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockUserProfileRepository = new Mock<IUserProfileRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<GetUserRolesQueryHandler>>();

        _handler = new GetUserRolesQueryHandler(
            _mockUserRoleRepository.Object,
            _mockUserProfileRepository.Object,
            _mockRoleRepository.Object,
            _mockMapper.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnUserRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var roleId1 = Guid.NewGuid();
        var roleId2 = Guid.NewGuid();

        var query = new GetUserRolesQuery { AuthUserId = authUserId };

        var user = new UserProfile { Id = userId, AuthUserId = authUserId };

        var role1 = new Role { Id = roleId1, Name = "Admin", Description = "Administrator role" };
        var role2 = new Role { Id = roleId2, Name = "User", Description = "Regular user role" };

        var userRoles = new List<UserRole>
        {
            new UserRole
            {
                Id = Guid.NewGuid(),
                AuthUserId = authUserId,
                RoleId = roleId1,
                AssignedAt = DateTime.UtcNow.AddDays(-1)
            },
            new UserRole
            {
                Id = Guid.NewGuid(),
                AuthUserId = authUserId,
                RoleId = roleId2,
                AssignedAt = DateTime.UtcNow.AddDays(-2)
            }
        };

        var expectedUserRoleDtos = new List<UserRoleDto>
        {
            new UserRoleDto
            {
                RoleId = roleId1,
                RoleName = "Admin",
                Description = "Administrator role",
                AssignedAt = userRoles[0].AssignedAt
            },
            new UserRoleDto
            {
                RoleId = roleId2,
                RoleName = "User",
                Description = "Regular user role",
                AssignedAt = userRoles[1].AssignedAt
            }
        };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(userRoles);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role1);

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role2);

        // Setup AutoMapper to return UserRoleDto for each UserRole
        _mockMapper.Setup(x => x.Map<UserRoleDto>(userRoles[0]))
            .Returns(expectedUserRoleDtos[0]);

        _mockMapper.Setup(x => x.Map<UserRoleDto>(userRoles[1]))
            .Returns(expectedUserRoleDtos[1]);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Roles.Should().HaveCount(2);

        var adminRole = result.Roles.First(r => r.RoleName == "Admin");
        adminRole.RoleId.Should().Be(roleId1);
        adminRole.Description.Should().Be("Administrator role");

        var userRole = result.Roles.First(r => r.RoleName == "User");
        userRole.RoleId.Should().Be(roleId2);
        userRole.Description.Should().Be("Regular user role");

        // Verify AutoMapper was called for each UserRole
        _mockMapper.Verify(x => x.Map<UserRoleDto>(It.IsAny<UserRole>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_UserNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var query = new GetUserRolesQuery { AuthUserId = Guid.NewGuid() };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(query.AuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserProfile?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, CancellationToken.None));

        exception.Message.Should().Contain("User");
        exception.Message.Should().Contain(query.AuthUserId.ToString());
    }

    [Fact]
    public async Task Handle_UserWithNoRoles_ShouldReturnEmptyRolesList()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();

        var query = new GetUserRolesQuery { AuthUserId = authUserId };

        var user = new UserProfile { Id = userId, AuthUserId = authUserId };

        _mockUserProfileRepository.Setup(x => x.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserId.Should().Be(userId);
        result.Roles.Should().BeEmpty();
    }
}