using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.AddSubjectToCurriculum;
using RogueLearn.User.Application.Features.CurriculumStructure.Commands.UpdateCurriculumStructure;
using RogueLearn.User.Application.Features.CurriculumStructure.Queries.GetCurriculumStructureByVersion;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumStructureControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ICurriculumStructureRepository> _mockCurriculumStructureRepository;
    private readonly Mock<ICurriculumVersionRepository> _mockCurriculumVersionRepository;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;

    public CurriculumStructureControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockCurriculumStructureRepository = new Mock<ICurriculumStructureRepository>();
        _mockCurriculumVersionRepository = new Mock<ICurriculumVersionRepository>();
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
                // Remove existing registrations
                var curriculumStructureDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurriculumStructureRepository));
                if (curriculumStructureDescriptor != null)
                    services.Remove(curriculumStructureDescriptor);

                var curriculumVersionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurriculumVersionRepository));
                if (curriculumVersionDescriptor != null)
                    services.Remove(curriculumVersionDescriptor);

                var subjectDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISubjectRepository));
                if (subjectDescriptor != null)
                    services.Remove(subjectDescriptor);

                // Replace with mocks (register under specific service types)
                services.AddScoped(typeof(ICurriculumStructureRepository), _ => _mockCurriculumStructureRepository.Object);
                services.AddScoped(typeof(ICurriculumVersionRepository), _ => _mockCurriculumVersionRepository.Object);
                services.AddScoped(typeof(ISubjectRepository), _ => _mockSubjectRepository.Object);

                // Relax JWT validation to accept locally minted test tokens
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
    public async Task GetByVersion_WithValidVersionId_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var versionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var curriculumStructures = new List<CurriculumStructure>
        {
            new CurriculumStructure
            {
                Id = Guid.NewGuid(),
                CurriculumVersionId = versionId,
                SubjectId = subjectId,
                TermNumber = 1,
                IsMandatory = true,
                PrerequisiteSubjectIds = new Guid[] { },
                PrerequisitesText = "",
                CreatedAt = DateTimeOffset.UtcNow
            }
        };

        var subject = new Subject
        {
            Id = subjectId,
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3,
            Description = "Basic computer science concepts"
        };

        _mockCurriculumStructureRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(curriculumStructures);

        _mockSubjectRepository.Setup(x => x.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subject);

        // Act
        var response = await _client.GetAsync($"/api/admin/curriculum-structure/version/{versionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<CurriculumStructureDto>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Should().HaveCount(1);
        result![0].SubjectId.Should().Be(subjectId);
        result![0].TermNumber.Should().Be(1);
        result![0].IsMandatory.Should().BeTrue();
    }

    [Fact]
    public async Task AddSubject_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var versionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var command = new AddSubjectToCurriculumCommand
        {
            CurriculumVersionId = versionId,
            SubjectId = subjectId,
            TermNumber = 1,
            IsMandatory = true,
            PrerequisiteSubjectIds = new Guid[] { },
            PrerequisitesText = ""
        };

        var curriculumVersion = new CurriculumVersion
        {
            Id = versionId,
            ProgramId = Guid.NewGuid(),
            VersionCode = "1.0",
            EffectiveYear = DateTime.UtcNow.Year,
            IsActive = true
        };

        var subject = new Subject
        {
            Id = subjectId,
            SubjectCode = "CS101",
            SubjectName = "Introduction to Computer Science",
            Credits = 3,
            Description = "Basic computer science concepts"
        };

        var createdStructure = new CurriculumStructure
         {
             Id = Guid.NewGuid(),
             CurriculumVersionId = versionId,
             SubjectId = subjectId,
             TermNumber = 1,
             IsMandatory = true,
             PrerequisiteSubjectIds = new Guid[] { },
             PrerequisitesText = "",
             CreatedAt = DateTimeOffset.UtcNow
         };

        _mockCurriculumVersionRepository.Setup(x => x.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(curriculumVersion);

        _mockSubjectRepository.Setup(x => x.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subject);

        _mockCurriculumStructureRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CurriculumStructure>());

        _mockCurriculumStructureRepository.Setup(x => x.AddAsync(It.IsAny<CurriculumStructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdStructure);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/curriculum-structure", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<AddSubjectToCurriculumResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.CurriculumVersionId.Should().Be(versionId);
        result.SubjectId.Should().Be(subjectId);
        result.TermNumber.Should().Be(1);
        result.IsMandatory.Should().BeTrue();
    }

    [Fact]
    public async Task AddSubject_WithInvalidCurriculumVersion_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var versionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var command = new AddSubjectToCurriculumCommand
        {
            CurriculumVersionId = versionId,
            SubjectId = subjectId,
            TermNumber = 1,
            IsMandatory = true,
            PrerequisiteSubjectIds = new Guid[] { },
            PrerequisitesText = ""
        };

        _mockCurriculumVersionRepository.Setup(x => x.GetByIdAsync(versionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumVersion?)null);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/admin/curriculum-structure", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithValidData_ShouldReturnOk()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var structureId = Guid.NewGuid();
        var command = new UpdateCurriculumStructureCommand
        {
            Id = structureId,
            TermNumber = 2,
            IsMandatory = false,
            PrerequisiteSubjectIds = new Guid[] { },
            PrerequisitesText = "Updated prerequisites"
        };

        var existingStructure = new CurriculumStructure
         {
             Id = structureId,
             CurriculumVersionId = Guid.NewGuid(),
             SubjectId = Guid.NewGuid(),
             TermNumber = 1,
             IsMandatory = true,
             PrerequisiteSubjectIds = new Guid[] { },
             PrerequisitesText = "",
             CreatedAt = DateTimeOffset.UtcNow
         };

        var updatedStructure = new CurriculumStructure
         {
             Id = structureId,
             CurriculumVersionId = existingStructure.CurriculumVersionId,
             SubjectId = existingStructure.SubjectId,
             TermNumber = 2,
             IsMandatory = false,
             PrerequisiteSubjectIds = new Guid[] { },
             PrerequisitesText = "Updated prerequisites",
             CreatedAt = existingStructure.CreatedAt
         };

        _mockCurriculumStructureRepository.Setup(x => x.GetByIdAsync(structureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStructure);

        _mockCurriculumStructureRepository.Setup(x => x.UpdateAsync(It.IsAny<CurriculumStructure>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedStructure);

        var json = JsonSerializer.Serialize(command);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PutAsync($"/api/admin/curriculum-structure/{structureId}", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<UpdateCurriculumStructureResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        result.Should().NotBeNull();
        result!.Id.Should().Be(structureId);
        result.TermNumber.Should().Be(2);
        result.IsMandatory.Should().BeFalse();
        result.PrerequisitesText.Should().Be("Updated prerequisites");
    }

    [Fact]
    public async Task Remove_WithValidId_ShouldReturnNoContent()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var structureId = Guid.NewGuid();
        var existingStructure = new CurriculumStructure
         {
             Id = structureId,
             CurriculumVersionId = Guid.NewGuid(),
             SubjectId = Guid.NewGuid(),
             TermNumber = 1,
             IsMandatory = true,
             PrerequisiteSubjectIds = new Guid[] { },
             PrerequisitesText = "",
             CreatedAt = DateTimeOffset.UtcNow
         };

        _mockCurriculumStructureRepository.Setup(x => x.GetByIdAsync(structureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStructure);

        _mockCurriculumStructureRepository.Setup(x => x.DeleteAsync(structureId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/curriculum-structure/{structureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Remove_WithInvalidId_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var structureId = Guid.NewGuid();

        _mockCurriculumStructureRepository.Setup(x => x.GetByIdAsync(structureId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumStructure?)null);

        // Act
        var response = await _client.DeleteAsync($"/api/admin/curriculum-structure/{structureId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByVersion_WithoutAdminRole_ShouldReturnForbidden()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "Student");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var versionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/admin/curriculum-structure/version/{versionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private string GenerateJwtToken(Guid userId, string role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
            new Claim("sub", userId.ToString())
        };

        var token = new JwtSecurityToken(
            issuer: "https://test.supabase.co/auth/v1",
            audience: "authenticated",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}