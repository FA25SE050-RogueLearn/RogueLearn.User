using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RogueLearn.User.Application.Features.Roles.Commands.CreateRole;
using RogueLearn.User.Application.Features.Roles.Commands.UpdateRole;
using RogueLearn.User.Application.Features.Roles.Commands.DeleteRole;
using RogueLearn.User.Application.Features.Roles.Queries.GetAllRoles;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class RolesControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IRoleRepository> _mockRoleRepository;
    private readonly Mock<IUserRoleRepository> _mockUserRoleRepository;

    public RolesControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockRoleRepository = new Mock<IRoleRepository>();
        _mockUserRoleRepository = new Mock<IUserRoleRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration for Supabase
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Supabase:Url"] = "https://test.supabase.co",
                    ["Supabase:ApiKey"] = "test-api-key",
                    ["Supabase:JwtSecret"] = "test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes",
                    ["AI:Provider"] = "Google",
                    ["AI:Google:Model"] = "gemini-1.5-flash",
                    ["AI:Google:ApiKey"] = "dummy-test-key"
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove existing registrations
                var roleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRoleRepository));
                if (roleDescriptor != null)
                    services.Remove(roleDescriptor);

                var userRoleDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IUserRoleRepository));
                if (userRoleDescriptor != null)
                    services.Remove(userRoleDescriptor);

                // Replace with mocks
                services.AddScoped(_ => _mockRoleRepository.Object);
                services.AddScoped(_ => _mockUserRoleRepository.Object);

                // Override JWT Bearer options to align with test configuration and avoid remote metadata
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = "https://test.supabase.co";
                    options.RequireHttpsMetadata = false;
                    options.Audience = "authenticated";
                    options.TokenValidationParameters.ValidIssuer = "https://test.supabase.co/auth/v1";
                    options.TokenValidationParameters.ValidAudience = "authenticated";
                    options.TokenValidationParameters.ValidateIssuer = false; // Relax issuer validation for tests
                    options.TokenValidationParameters.ValidateAudience = false; // Relax audience validation for tests
                    options.TokenValidationParameters.ValidateLifetime = true;
                    options.TokenValidationParameters.ValidateIssuerSigningKey = true;
                    options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"));
                    options.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                });
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task GetAllRoles_WithAdminRole_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roles = new List<Role>
        {
            new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "Administrator role" },
            new Role { Id = Guid.NewGuid(), Name = "User", Description = "Regular user role" }
        };

        _mockRoleRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var response = await _client.GetAsync("/api/admin/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<GetAllRolesResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Roles.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllRoles_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync("/api/admin/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateRole_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();
        var createRoleCommand = new CreateRoleCommand
        {
            Name = "TestRole",
            Description = "Test role description"
        };

        var createdRole = new Role
        {
            Id = roleId,
            Name = createRoleCommand.Name,
            Description = createRoleCommand.Description,
            CreatedAt = DateTime.UtcNow
        };

        _mockRoleRepository.Setup(x => x.GetByNameAsync(createRoleCommand.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRoleRepository.Setup(x => x.AddAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdRole);

        var json = JsonSerializer.Serialize(createRoleCommand);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/roles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateRoleResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Name.Should().Be(createRoleCommand.Name);
        result.Description.Should().Be(createRoleCommand.Description);
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ShouldReturnBadRequest()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRoleCommand = new CreateRoleCommand
        {
            Name = "ExistingRole",
            Description = "Test role description"
        };

        var existingRole = new Role
        {
            Id = Guid.NewGuid(),
            Name = createRoleCommand.Name,
            Description = "Existing role"
        };

        _mockRoleRepository.Setup(x => x.GetByNameAsync(createRoleCommand.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        var json = JsonSerializer.Serialize(createRoleCommand);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/roles", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRole_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();
        var updateRoleCommand = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "UpdatedRole",
            Description = "Updated role description"
        };

        var existingRole = new Role
        {
            Id = roleId,
            Name = "OldRole",
            Description = "Old description",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updatedRole = new Role
        {
            Id = roleId,
            Name = updateRoleCommand.Name,
            Description = updateRoleCommand.Description,
            CreatedAt = existingRole.CreatedAt
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockRoleRepository.Setup(x => x.GetByNameAsync(updateRoleCommand.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRoleRepository.Setup(x => x.UpdateAsync(It.IsAny<Role>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedRole);

        var json = JsonSerializer.Serialize(updateRoleCommand);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/admin/roles/{roleId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UpdateRoleResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Name.Should().Be(updateRoleCommand.Name);
        result.Description.Should().Be(updateRoleCommand.Description);
    }

    [Fact]
    public async Task UpdateRole_WithNonExistentRole_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();
        var updateRoleCommand = new UpdateRoleCommand
        {
            Id = roleId,
            Name = "UpdatedRole",
            Description = "Updated role description"
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        var json = JsonSerializer.Serialize(updateRoleCommand);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/admin/roles/{roleId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "TestRole",
            Description = "Test role"
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserRole>());

        _mockRoleRepository.Setup(x => x.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteRole_WithNonExistentRole_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteRole_WithAssignedUsers_ShouldReturnBadRequest()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var roleId = Guid.NewGuid();
        var existingRole = new Role
        {
            Id = roleId,
            Name = "TestRole",
            Description = "Test role"
        };

        var assignedUsers = new List<UserRole>
        {
            new UserRole { AuthUserId = Guid.NewGuid(), RoleId = roleId }
        };

        _mockRoleRepository.Setup(x => x.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingRole);

        _mockUserRoleRepository.Setup(x => x.GetUsersByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assignedUsers);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AllEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        // Arrange - No authentication header set

        // Act & Assert
        var getResponse = await _client.GetAsync("/api/admin/roles");
        getResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var createContent = new StringContent(JsonSerializer.Serialize(new CreateRoleCommand { Name = "Test", Description = "Test" }), Encoding.UTF8, "application/json");
        var postResponse = await _client.PostAsync("/api/admin/roles", createContent);
        postResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var updateContent = new StringContent(JsonSerializer.Serialize(new UpdateRoleCommand { Id = Guid.NewGuid(), Name = "Test", Description = "Test" }), Encoding.UTF8, "application/json");
        var putResponse = await _client.PutAsync($"/api/admin/roles/{Guid.NewGuid()}", updateContent);
        putResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var deleteResponse = await _client.DeleteAsync($"/api/admin/roles/{Guid.NewGuid()}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAllRoles_WithNonAdminUser_ShouldReturn403()
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
        var response = await _client.GetAsync("/api/admin/roles");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
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
                new Claim(ClaimTypes.NameIdentifier, authUserId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim("role", role) // Additional role claim for compatibility
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