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
using Microsoft.AspNetCore.Authentication.JwtBearer;
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
                services.AddScoped<ICurriculumProgramRepository>(_ => _mockCurriculumProgramRepository.Object);
                services.AddScoped<ICurriculumVersionRepository>(_ => _mockCurriculumVersionRepository.Object);
                services.AddScoped<ISubjectRepository>(_ => _mockSubjectRepository.Object);
                services.AddScoped<ISyllabusVersionRepository>(_ => _mockSyllabusVersionRepository.Object);

                // Override FLM extraction plugin with a deterministic test stub
                var flmDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(RogueLearn.User.Application.Plugins.IFlmExtractionPlugin));
                if (flmDescriptor != null)
                    services.Remove(flmDescriptor);

                var flmMock = new Moq.Mock<RogueLearn.User.Application.Plugins.IFlmExtractionPlugin>();

                flmMock.Setup(x => x.ExtractCurriculumJsonAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Returns<string, System.Threading.CancellationToken>((rawText, ct) =>
                    {
                        var hasInvalid = rawText.Contains("Invalid curriculum format", StringComparison.OrdinalIgnoreCase);
                        var curriculumJson = hasInvalid
                            ? "{\"program\":{\"programName\":\"Computer Science\",\"programCode\":\"CS2024\",\"degreeLevel\":\"Bachelor\",\"totalCredits\":120,\"durationYears\":4},\"version\":{\"versionCode\":\"2024.1\",\"effectiveYear\":2024,\"isActive\":true},\"subjects\":[],\"structure\":[{\"subjectCode\":\"CS101\",\"termNumber\":1,\"isMandatory\":true}]}"
                            : "{\"program\":{\"programName\":\"Computer Science\",\"programCode\":\"CS2024\",\"degreeLevel\":\"Bachelor\",\"totalCredits\":120,\"durationYears\":4},\"version\":{\"versionCode\":\"2024.1\",\"effectiveYear\":2024,\"isActive\":true},\"subjects\":[{\"subjectCode\":\"CS101\",\"subjectName\":\"Intro to Programming\",\"credits\":3},{\"subjectCode\":\"CS102\",\"subjectName\":\"Data Structures\",\"credits\":3},{\"subjectCode\":\"CS201\",\"subjectName\":\"Algorithms\",\"credits\":4}],\"structure\":[{\"subjectCode\":\"CS101\",\"termNumber\":1,\"isMandatory\":true},{\"subjectCode\":\"CS102\",\"termNumber\":2,\"isMandatory\":true},{\"subjectCode\":\"CS201\",\"termNumber\":3,\"isMandatory\":true}]}";
                        return Task.FromResult(curriculumJson);
                    });

                flmMock.Setup(x => x.ExtractSyllabusJsonAsync(It.IsAny<string>(), It.IsAny<System.Threading.CancellationToken>()))
                    .Returns<string, System.Threading.CancellationToken>((rawText, ct) =>
                    {
                        // Extract subject code from text if present
                        var subjectCode = "CS101";
                        try
                        {
                            var marker = "Subject Code:";
                            var idx = rawText.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                            if (idx >= 0)
                            {
                                var rest = rawText[(idx + marker.Length)..].Trim();
                                var firstLine = rest.Split('\n', '\r')[0].Trim();
                                subjectCode = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                            }
                        }
                        catch { }

                        var syllabusJson = "{\"subjectCode\":\"" + subjectCode + "\",\"syllabusName\":\"Introduction to Programming\",\"versionNumber\":1,\"noCredit\":3,\"degreeLevel\":\"Bachelor\",\"description\":\"Intro programming course\",\"content\":{\"courseDescription\":\"Intro to programming\",\"weeklySchedule\":[{\"weekNumber\":1,\"topic\":\"Introduction\",\"activities\":[],\"readings\":[]},{\"weekNumber\":2,\"topic\":\"Variables\",\"activities\":[],\"readings\":[]}]}}";
                        return Task.FromResult(syllabusJson);
                    });

                services.AddSingleton<RogueLearn.User.Application.Plugins.IFlmExtractionPlugin>(flmMock.Object);

                // Relax JWT validation to accept locally minted test tokens
                services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = false,
                        ValidIssuer = "test-issuer",
                        ValidAudience = "test-audience",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"))
                    };
                });
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