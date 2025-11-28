using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RogueLearn.User.Application.Features.UserRoles.Commands.AssignRoleToUser;
using RogueLearn.User.Application.Features.UserRoles.Commands.RemoveRoleFromUser;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class UserRoleControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;
    private readonly Mock<IRoleRepository> _mockRoleRepository;

    public UserRoleControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();
        _mockRoleRepository = new Mock<IRoleRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration for Supabase
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Supabase:Url"] = "https://test.supabase.co",
                    ["Supabase:ApiKey"] = "test-api-key",
                    ["Supabase:JwtSecret"] = "test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var userRoleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRoleRepository));
                if (userRoleDescriptor != null)
                    services.Remove(userRoleDescriptor);

                var roleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRoleRepository));
                if (roleDescriptor != null)
                    services.Remove(roleDescriptor);

                // Replace with mocks
                services.AddScoped(_ => _mockUserRoleRepository.Object);
                services.AddScoped(_ => _mockRoleRepository.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task AssignRole_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var request = new AssignRoleToUserCommand
        {
            AuthUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/UserRole/assign", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssignRole_WithNonAdminUser_ShouldReturn403()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Setup mock to return non-admin roles
        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new UserRole
                {
                    RoleId = userRoleId
                }
            });

        // Setup role repository to return a non-admin role
        _mockRoleRepository.Setup(x => x.GetByIdAsync(userRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = userRoleId, Name = "User", Description = "Regular user" });

        var request = new AssignRoleToUserCommand
        {
            AuthUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/UserRole/assign", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemoveRole_WithNonAdminUser_ShouldReturn403()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Setup mock to return non-admin roles
        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new UserRole
                {
                    RoleId = userRoleId
                }
            });

        // Setup role repository to return a non-admin role
        _mockRoleRepository.Setup(x => x.GetByIdAsync(userRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = userRoleId, Name = "User", Description = "Regular user" });

        var request = new RemoveRoleFromUserCommand
        {
            AuthUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/UserRole/remove", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUserRoles_WithNonAdminUser_ShouldReturn403()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var userRoleId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Setup mock to return non-admin roles
        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new UserRole
                {
                    RoleId = userRoleId
                }
            });

        // Setup role repository to return a non-admin role
        _mockRoleRepository.Setup(x => x.GetByIdAsync(userRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = userRoleId, Name = "User", Description = "Regular user" });

        // Act
        var response = await _client.GetAsync($"/api/UserRole/user/{Guid.NewGuid()}/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignRole_WithAdminUser_ShouldNotReturn403()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Setup mock to return admin role
        _mockUserRoleRepository.Setup(x => x.GetRolesForUserAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>
            {
                new UserRole
                {
                    RoleId = adminRoleId
                }
            });

        // Setup role repository to return an admin role
        _mockRoleRepository.Setup(x => x.GetByIdAsync(adminRoleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Role { Id = adminRoleId, Name = "Game Master", Description = "Game Master role" });

        var request = new AssignRoleToUserCommand
        {
            AuthUserId = Guid.NewGuid(),
            RoleId = Guid.NewGuid()
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/UserRole/remove", content);

        // Assert
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        // Note: This might return 400 or 500 due to business logic validation, but not 403
    }

    private string GenerateJwtToken(Guid authUserId, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes");
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", authUserId.ToString()),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://test.supabase.co/auth/v1",
            Audience = "authenticated",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}