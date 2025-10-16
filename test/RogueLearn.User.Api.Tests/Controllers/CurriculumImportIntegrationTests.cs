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
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class CurriculumImportIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<ICurriculumProgramRepository> _mockCurriculumProgramRepository;
    private readonly Mock<ICurriculumVersionRepository> _mockCurriculumVersionRepository;
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ISyllabusVersionRepository> _mockSyllabusVersionRepository;

    public CurriculumImportIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _mockCurriculumProgramRepository = new Mock<ICurriculumProgramRepository>();
        _mockCurriculumVersionRepository = new Mock<ICurriculumVersionRepository>();
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockSyllabusVersionRepository = new Mock<ISyllabusVersionRepository>();

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
                var curriculumProgramDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurriculumProgramRepository));
                if (curriculumProgramDescriptor != null)
                    services.Remove(curriculumProgramDescriptor);

                var curriculumVersionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ICurriculumVersionRepository));
                if (curriculumVersionDescriptor != null)
                    services.Remove(curriculumVersionDescriptor);

                var subjectDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISubjectRepository));
                if (subjectDescriptor != null)
                    services.Remove(subjectDescriptor);

                var syllabusVersionDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ISyllabusVersionRepository));
                if (syllabusVersionDescriptor != null)
                    services.Remove(syllabusVersionDescriptor);

                // Replace with mocks
                services.AddScoped(_ => _mockCurriculumProgramRepository.Object);
                services.AddScoped(_ => _mockCurriculumVersionRepository.Object);
                services.AddScoped(_ => _mockSubjectRepository.Object);
                services.AddScoped(_ => _mockSyllabusVersionRepository.Object);
            });
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task ImportCurriculum_WithValidData_ShouldReturnSuccessAndPersistData()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var validCurriculumText = @"
            Program: Computer Science
            Version: 2024.1
            Effective Date: 2024-01-01
            
            Subjects:
            - CS101: Introduction to Programming (3 credits)
            - CS102: Data Structures (3 credits)
            - CS201: Algorithms (4 credits)
        ";

        var requestContent = new
        {
            RawText = validCurriculumText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Setup mocks
        _mockCurriculumProgramRepository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<CurriculumProgram, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumProgram?)null);

        _mockCurriculumProgramRepository.Setup(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumProgram { Id = Guid.NewGuid(), ProgramCode = "CS2024", ProgramName = "Computer Science" });

        _mockCurriculumVersionRepository.Setup(x => x.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumVersion { Id = Guid.NewGuid(), VersionCode = "2024.1", EffectiveYear = 2024 });

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/import", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("successfully imported");

        // Verify repository interactions
        _mockCurriculumProgramRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCurriculumVersionRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportCurriculum_WithInvalidData_ShouldReturnValidationErrors()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var invalidCurriculumText = @"
            Invalid curriculum format without required fields
        ";

        var requestContent = new
        {
            RawText = invalidCurriculumText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/import", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("success").GetBoolean().Should().BeFalse();
        result.GetProperty("errors").EnumerateArray().Should().NotBeEmpty();

        // Verify no data was persisted
        _mockCurriculumProgramRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockCurriculumVersionRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportSyllabus_WithValidData_ShouldReturnSuccessAndPersistData()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var subjectId = Guid.NewGuid();
        var validSyllabusText = @"
            Subject Code: CS101
            Version: 1.0
            Effective Date: 2024-01-01
            
            Course Description: Introduction to Programming
            Learning Outcomes:
            - Understand basic programming concepts
            - Write simple programs
            
            Weekly Schedule:
            Week 1: Introduction to Programming
            Week 2: Variables and Data Types
        ";

        var requestContent = new
        {
            RawText = validSyllabusText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Setup mocks
        var existingSubject = new Subject
        {
            Id = subjectId,
            SubjectCode = "CS101",
            SubjectName = "Introduction to Programming",
            Credits = 3
        };

        _mockSubjectRepository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubject);

        _mockSyllabusVersionRepository.Setup(x => x.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyllabusVersion { Id = Guid.NewGuid(), VersionNumber = 1 });

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/import-syllabus", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("success").GetBoolean().Should().BeTrue();
        result.GetProperty("message").GetString().Should().Contain("successfully imported");

        // Verify repository interactions
        _mockSubjectRepository.Verify(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSyllabusVersionRepository.Verify(x => x.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportSyllabus_WithNonExistentSubject_ShouldReturnNotFound()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var validSyllabusText = @"
            Subject Code: CS999
            Version: 1.0
            Effective Date: 2024-01-01
            
            Course Description: Non-existent Subject
        ";

        var requestContent = new
        {
            RawText = validSyllabusText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Setup mocks
        _mockSubjectRepository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<Subject, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Subject?)null);

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/import-syllabus", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("success").GetBoolean().Should().BeFalse();
        result.GetProperty("message").GetString().Should().Contain("Subject with code 'CS999' not found");

        // Verify no syllabus was created
        _mockSyllabusVersionRepository.Verify(x => x.AddAsync(It.IsAny<SyllabusVersion>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateCurriculum_WithValidData_ShouldReturnValidationSuccess()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var validCurriculumText = @"
            Program: Computer Science
            Version: 2024.1
            Effective Date: 2024-01-01
            
            Subjects:
            - CS101: Introduction to Programming (3 credits)
            - CS102: Data Structures (3 credits)
        ";

        var requestContent = new
        {
            RawText = validCurriculumText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/validate", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("isValid").GetBoolean().Should().BeTrue();
        result.GetProperty("validationErrors").EnumerateArray().Should().BeEmpty();
        result.TryGetProperty("extractedData", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ValidateSyllabus_WithValidData_ShouldReturnValidationSuccess()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var validSyllabusText = @"
            Subject Code: CS101
            Version: 1.0
            Effective Date: 2024-01-01
            
            Course Description: Introduction to Programming
            Learning Outcomes:
            - Understand basic programming concepts
        ";

        var requestContent = new
        {
            RawText = validSyllabusText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/validate-syllabus", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(responseContent);
        
        result.GetProperty("isValid").GetBoolean().Should().BeTrue();
        result.GetProperty("validationErrors").EnumerateArray().Should().BeEmpty();
        result.TryGetProperty("extractedData", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ImportWorkflow_EndToEnd_ShouldValidateAndImportSuccessfully()
    {
        // Arrange
        var adminUserId = Guid.NewGuid();
        var token = GenerateJwtToken(adminUserId, "Game Master");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var validCurriculumText = @"
            Program: Software Engineering
            Version: 2024.2
            Effective Date: 2024-02-01
            
            Subjects:
            - SE101: Software Engineering Fundamentals (3 credits)
            - SE102: Requirements Engineering (3 credits)
        ";

        var requestContent = new
        {
            RawText = validCurriculumText
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Setup mocks for import
        _mockCurriculumProgramRepository.Setup(x => x.FirstOrDefaultAsync(It.IsAny<Expression<Func<CurriculumProgram, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CurriculumProgram?)null);

        _mockCurriculumProgramRepository.Setup(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumProgram { Id = Guid.NewGuid(), ProgramCode = "SE2024", ProgramName = "Software Engineering" });

        _mockCurriculumVersionRepository.Setup(x => x.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CurriculumVersion { Id = Guid.NewGuid(), VersionCode = "2024.2", EffectiveYear = 2024 });

        // Act & Assert - Step 1: Validate
        var validateResponse = await _client.PostAsync("/api/curriculum-import/validate", content);
        validateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var validateContent = await validateResponse.Content.ReadAsStringAsync();
        var validateResult = JsonSerializer.Deserialize<JsonElement>(validateContent);
        validateResult.GetProperty("isValid").GetBoolean().Should().BeTrue();

        // Act & Assert - Step 2: Import
        var importResponse = await _client.PostAsync("/api/curriculum-import/import", content);
        importResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var importContent = await importResponse.Content.ReadAsStringAsync();
        var importResult = JsonSerializer.Deserialize<JsonElement>(importContent);
        importResult.GetProperty("success").GetBoolean().Should().BeTrue();

        // Verify complete workflow
        _mockCurriculumProgramRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumProgram>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCurriculumVersionRepository.Verify(x => x.AddAsync(It.IsAny<CurriculumVersion>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ImportCurriculum_WithoutAuthorization_ShouldReturnUnauthorized()
    {
        // Arrange
        var requestContent = new
        {
            RawText = "Some curriculum text"
        };

        var json = JsonSerializer.Serialize(requestContent);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/curriculum-import/import", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            issuer: "test-issuer",
            audience: "test-audience",
            claims: claims,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}