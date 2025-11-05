// test/RogueLearn.User.Api.Tests/Controllers/SyllabusVersionsControllerTests.cs
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.CreateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Commands.UpdateSyllabusVersion;
using RogueLearn.User.Application.Features.SyllabusVersions.Queries.GetSyllabusVersionsBySubject;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace RogueLearn.User.Api.Tests.Controllers;

public class SyllabusVersionsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ISyllabusVersionRepository> _mockSyllabusVersionRepository;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;

    public SyllabusVersionsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockSyllabusVersionRepository = new Mock<ISyllabusVersionRepository>();
        _mockSubjectRepository = new Mock<ISubjectRepository>();

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Supabase:Url"] = "https://test.supabase.co",
                    ["Supabase:ApiKey"] = "test-api-key",
                    ["Supabase:JwtSecret"] = "test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"
                });
            });

            builder.ConfigureServices(services =>
            {
                var syllabusVersionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISyllabusVersionRepository));
                if (syllabusVersionDescriptor != null)
                    services.Remove(syllabusVersionDescriptor);

                var subjectDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISubjectRepository));
                if (subjectDescriptor != null)
                    services.Remove(subjectDescriptor);

                services.AddScoped(typeof(ISyllabusVersionRepository), _ => _mockSyllabusVersionRepository.Object);
                services.AddScoped(typeof(ISubjectRepository), _ => _mockSubjectRepository.Object);

                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.Authority = "https://test.supabase.co";
                    options.RequireHttpsMetadata = false;
                    options.Audience = "authenticated";
                    options.TokenValidationParameters.ValidIssuer = "https://test.supabase.co/auth/v1";
                    options.TokenValidationParameters.ValidAudience = "authenticated";
                    options.TokenValidationParameters.ValidateIssuer = false;
                    options.TokenValidationParameters.ValidateAudience = false;
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
    public async Task GetBySubject_WithValidSubjectId_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();
        var syllabusVersions = new List<SyllabusVersion>
        {
            new SyllabusVersion
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                VersionNumber = 1,
                // MODIFICATION: Use a Dictionary to match the entity's property type.
                Content = new Dictionary<string, object> { { "summary", "Initial syllabus content" } },
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
                IsActive = false,
                CreatedBy = adminUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-30)
            },
            new SyllabusVersion
            {
                Id = Guid.NewGuid(),
                SubjectId = subjectId,
                VersionNumber = 2,
                // MODIFICATION: Use a Dictionary to match the entity's property type.
                Content = new Dictionary<string, object> { { "summary", "Updated syllabus content" } },
                EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
                IsActive = true,
                CreatedBy = adminUserId,
                CreatedAt = DateTime.UtcNow
            }
        };

        _mockSyllabusVersionRepository.Setup(x => x.FindAsync(
             It.Is<Expression<Func<SyllabusVersion, bool>>>(expr => true),
             It.IsAny<CancellationToken>()))
             .ReturnsAsync(syllabusVersions);

        // Act
        var response = await _client.GetAsync($"/api/admin/syllabus-versions/subject/{subjectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<SyllabusVersionDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
        result![0].SubjectId.Should().Be(subjectId);
        result![1].SubjectId.Should().Be(subjectId);
    }

    [Fact]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();
        // MODIFICATION: The Content in the command is now a JSON string.
        var commandContent = new { summary = "Initial syllabus content" };
        var command = new CreateSyllabusVersionCommand
        {
            SubjectId = subjectId,
            VersionNumber = 1,
            Content = JsonSerializer.Serialize(commandContent),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId
        };

        var subject = new Subject
        {
            Id = subjectId,
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3,
            Description = "Basic computer science concepts"
        };

        var createdSyllabusVersion = new SyllabusVersion
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            VersionNumber = 1,
            // MODIFICATION: Use a Dictionary to match the entity's property type.
            Content = new Dictionary<string, object> { { "summary", "Initial syllabus content" } },
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockSubjectRepository.Setup(x => x.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subject);

        _mockSyllabusVersionRepository.Setup(x => x.FindAsync(
             It.Is<Expression<Func<SyllabusVersion, bool>>>(expr => true),
             It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<SyllabusVersion>());

        _mockSyllabusVersionRepository.Setup(x => x.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdSyllabusVersion);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/syllabus-versions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateSyllabusVersionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.SubjectId.Should().Be(subjectId);
        result.VersionNumber.Should().Be(1);
        // MODIFICATION: Assert that the response Content is a valid serialized JSON string.
        result.Content.Should().Contain("\"summary\":\"Initial syllabus content\"");
        result.IsActive.Should().BeTrue();
        result.CreatedBy.Should().Be(adminUserId);
    }

    [Fact]
    public async Task Create_WithInvalidSubject_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();
        // MODIFICATION: The Content in the command is now a JSON string.
        var commandContent = new { summary = "Initial syllabus content" };
        var command = new CreateSyllabusVersionCommand
        {
            SubjectId = subjectId,
            VersionNumber = 1,
            Content = JsonSerializer.Serialize(commandContent),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId
        };

        _mockSubjectRepository.Setup(x => x.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subject?)null);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/syllabus-versions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithDuplicateVersion_ShouldReturnConflict()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();
        // MODIFICATION: The Content in the command is now a JSON string.
        var commandContent = new { summary = "Initial syllabus content" };
        var command = new CreateSyllabusVersionCommand
        {
            SubjectId = subjectId,
            VersionNumber = 1,
            Content = JsonSerializer.Serialize(commandContent),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId
        };

        var subject = new Subject
        {
            Id = subjectId,
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3,
            Description = "Basic computer science concepts"
        };

        var existingSyllabusVersion = new SyllabusVersion
        {
            Id = Guid.NewGuid(),
            SubjectId = subjectId,
            VersionNumber = 1,
            // MODIFICATION: Use a Dictionary to match the entity's property type.
            Content = new Dictionary<string, object> { { "summary", "Existing syllabus content" } },
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-30)),
            IsActive = false,
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        _mockSubjectRepository.Setup(x => x.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subject);

        _mockSyllabusVersionRepository.Setup(x => x.FindAsync(
             It.Is<Expression<Func<SyllabusVersion, bool>>>(expr => true),
             It.IsAny<CancellationToken>()))
             .ReturnsAsync(new List<SyllabusVersion> { existingSyllabusVersion });

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/syllabus-versions", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var syllabusVersionId = Guid.NewGuid();
        // MODIFICATION: The Content in the command is now a JSON string.
        var commandContent = new { summary = "Updated syllabus content" };
        var command = new UpdateSyllabusVersionCommand
        {
            Id = syllabusVersionId,
            Content = JsonSerializer.Serialize(commandContent),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            IsActive = false
        };

        var existingSyllabusVersion = new SyllabusVersion
        {
            Id = syllabusVersionId,
            SubjectId = Guid.NewGuid(),
            VersionNumber = 1,
            // MODIFICATION: Use a Dictionary to match the entity's property type.
            Content = new Dictionary<string, object> { { "summary", "Original syllabus content" } },
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updatedSyllabusVersion = new SyllabusVersion
        {
            Id = syllabusVersionId,
            SubjectId = existingSyllabusVersion.SubjectId,
            VersionNumber = existingSyllabusVersion.VersionNumber,
            // MODIFICATION: Use a Dictionary to match the entity's property type.
            Content = new Dictionary<string, object> { { "summary", "Updated syllabus content" } },
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            IsActive = false,
            CreatedBy = existingSyllabusVersion.CreatedBy,
            CreatedAt = existingSyllabusVersion.CreatedAt
        };

        _mockSyllabusVersionRepository.Setup(x => x.GetByIdAsync(syllabusVersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSyllabusVersion);

        _mockSyllabusVersionRepository.Setup(x => x.UpdateAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedSyllabusVersion);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/admin/syllabus-versions/{syllabusVersionId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UpdateSyllabusVersionResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        result.Should().NotBeNull();
        result!.Id.Should().Be(syllabusVersionId);
        // MODIFICATION: Assert that the response Content is a valid serialized JSON string.
        result.Content.Should().Contain("\"summary\":\"Updated syllabus content\"");
        result.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Update_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var syllabusVersionId = Guid.NewGuid();
        // MODIFICATION: The Content in the command is now a JSON string.
        var commandContent = new { summary = "Updated syllabus content" };
        var command = new UpdateSyllabusVersionCommand
        {
            Id = syllabusVersionId,
            Content = JsonSerializer.Serialize(commandContent),
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            IsActive = false
        };

        _mockSyllabusVersionRepository.Setup(x => x.GetByIdAsync(syllabusVersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyllabusVersion?)null);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/admin/syllabus-versions/{syllabusVersionId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var syllabusVersionId = Guid.NewGuid();
        var existingSyllabusVersion = new SyllabusVersion
        {
            Id = syllabusVersionId,
            SubjectId = Guid.NewGuid(),
            VersionNumber = 1,
            // MODIFICATION: Use a Dictionary to match the entity's property type.
            Content = new Dictionary<string, object> { { "summary", "Syllabus content" } },
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true,
            CreatedBy = adminUserId,
            CreatedAt = DateTime.UtcNow
        };

        _mockSyllabusVersionRepository.Setup(x => x.GetByIdAsync(syllabusVersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSyllabusVersion);

        _mockSyllabusVersionRepository.Setup(x => x.DeleteAsync(syllabusVersionId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/syllabus-versions/{syllabusVersionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var syllabusVersionId = Guid.NewGuid();

        _mockSyllabusVersionRepository.Setup(x => x.GetByIdAsync(syllabusVersionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SyllabusVersion?)null);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/syllabus-versions/{syllabusVersionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySubject_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "Student");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/admin/syllabus-versions/subject/{subjectId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string GenerateJwtToken(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim("sub", userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: "https://test.supabase.co/auth/v1",
            audience: "authenticated",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}