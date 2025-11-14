// test/RogueLearn.User.Api.Tests/Controllers/QuestsControllerTests.cs
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
using RogueLearn.User.Application.Features.Quests.Commands.GenerateQuestSteps;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Plugins;
using RogueLearn.User.Application.Services;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class QuestsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    // Mocks for all dependencies of the command handler
    private readonly Mock<IQuestRepository> _mockQuestRepository = new();
    private readonly Mock<IQuestStepRepository> _mockQuestStepRepository = new();
    private readonly Mock<ISubjectRepository> _mockSubjectRepository = new();
    private readonly Mock<IQuestGenerationPlugin> _mockQuestGenerationPlugin = new();
    private readonly Mock<IUserProfileRepository> _mockUserProfileRepository = new();
    private readonly Mock<IClassRepository> _mockClassRepository = new();
    private readonly Mock<ISkillRepository> _mockSkillRepository = new();
    private readonly Mock<ISubjectSkillMappingRepository> _mockSubjectSkillMappingRepository = new();
    private readonly Mock<IPromptBuilder> _mockPromptBuilder = new();

    public QuestsControllerTests(WebApplicationFactory<Program> factory)
    {
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
                // Replace all repository and plugin dependencies with mocks
                ReplaceService(services, typeof(IQuestRepository), _mockQuestRepository.Object);
                ReplaceService(services, typeof(IQuestStepRepository), _mockQuestStepRepository.Object);
                ReplaceService(services, typeof(ISubjectRepository), _mockSubjectRepository.Object);
                ReplaceService(services, typeof(IQuestGenerationPlugin), _mockQuestGenerationPlugin.Object);
                ReplaceService(services, typeof(IUserProfileRepository), _mockUserProfileRepository.Object);
                ReplaceService(services, typeof(IClassRepository), _mockClassRepository.Object);
                ReplaceService(services, typeof(ISkillRepository), _mockSkillRepository.Object);
                ReplaceService(services, typeof(ISubjectSkillMappingRepository), _mockSubjectSkillMappingRepository.Object);
                ReplaceService(services, typeof(IPromptBuilder), _mockPromptBuilder.Object);

                // Configure JWT authentication for testing
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

    private static void ReplaceService(IServiceCollection services, Type serviceType, object implementation)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);
        if (descriptor != null)
            services.Remove(descriptor);
        services.AddScoped(serviceType, _ => implementation);
    }

    private static string GenerateJwtToken(Guid authUserId, string[]? roles = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes");

        var claims = new List<Claim>
        {
            new("sub", authUserId.ToString()),
            new("auth_user_id", authUserId.ToString()),
            new(ClaimTypes.NameIdentifier, authUserId.ToString())
        };

        if (roles != null && roles.Length > 0)
        {
            claims.Add(new Claim("roles", string.Join(',', roles)));
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "https://test.supabase.co/auth/v1",
            Audience = "authenticated",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task GenerateQuestSteps_WhenQuestIsValid_ShouldCreateStepsAndReturn201Created()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var questId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var validSkillId1 = Guid.NewGuid();
        var validSkillId2 = Guid.NewGuid();
        var invalidSkillId = Guid.NewGuid();

        // 1. Authenticate the request
        var token = GenerateJwtToken(authUserId);
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 2. Mock all dependencies for the handler's successful execution path
        _mockQuestStepRepository.Setup(r => r.QuestContainsSteps(questId, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _mockUserProfileRepository.Setup(r => r.GetByAuthIdAsync(authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile { ClassId = classId });
        _mockClassRepository.Setup(r => r.GetByIdAsync(classId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Class());
        _mockQuestRepository.Setup(r => r.GetByIdAsync(questId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Quest { Id = questId, SubjectId = subjectId });
        _mockSubjectRepository.Setup(r => r.GetByIdAsync(subjectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Subject { Id = subjectId, Content = new Dictionary<string, object> { { "description", "Test syllabus" } } });
        _mockSubjectSkillMappingRepository.Setup(r => r.GetMappingsBySubjectIdsAsync(It.Is<IEnumerable<Guid>>(ids => ids.Contains(subjectId)), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SubjectSkillMapping>
            {
                new() { SkillId = validSkillId1 },
                new() { SkillId = validSkillId2 }
            });
        _mockSkillRepository.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Skill>
            {
                new() { Id = validSkillId1, Name = "Valid Skill 1" },
                new() { Id = validSkillId2, Name = "Valid Skill 2" }
            });
        _mockPromptBuilder.Setup(b => b.GenerateAsync(It.IsAny<UserProfile>(), It.IsAny<Class>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test user context");

        // Mock the AI response with two valid steps and one invalid step (to test filtering)
        var aiJsonResponse = JsonSerializer.Serialize(new[]
        {
            new { stepNumber = 1, title = "Step 1", description = "Valid Step 1", stepType = "Reading", experiencePoints = 10, content = new { skillId = validSkillId1.ToString() } },
            new { stepNumber = 2, title = "Step 2", description = "Invalid Step", stepType = "Quiz", experiencePoints = 20, content = new { skillId = invalidSkillId.ToString() } },
            new { stepNumber = 3, title = "Step 3", description = "Valid Step 2", stepType = "Coding", experiencePoints = 30, content = new { skillId = validSkillId2.ToString() } }
        });
        _mockQuestGenerationPlugin.Setup(p => p.GenerateQuestStepsJsonAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<Skill>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(aiJsonResponse);

        // Mock the repository 'AddAsync' to confirm it's being called
        _mockQuestStepRepository.Setup(r => r.AddAsync(It.IsAny<QuestStep>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestStep step, CancellationToken _) => step);

        // Act
        var response = await _client.PostAsync($"/api/quests/{questId}/generate-steps", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var responseBody = await response.Content.ReadAsStringAsync();
        var generatedSteps = JsonSerializer.Deserialize<List<GeneratedQuestStepDto>>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        // The handler should have filtered out the step with the invalid skillId
        generatedSteps.Should().NotBeNull();
        generatedSteps.Should().HaveCount(2);
        generatedSteps.Should().Contain(s => s.Title == "Step 1");
        generatedSteps.Should().Contain(s => s.Title == "Step 3");
        generatedSteps.Should().NotContain(s => s.Title == "Step 2");

        // Verify that the repository was only called for the valid steps
        _mockQuestStepRepository.Verify(r => r.AddAsync(It.Is<QuestStep>(s => s.SkillId == validSkillId1), It.IsAny<CancellationToken>()), Times.Once);
        _mockQuestStepRepository.Verify(r => r.AddAsync(It.Is<QuestStep>(s => s.SkillId == validSkillId2), It.IsAny<CancellationToken>()), Times.Once);
        _mockQuestStepRepository.Verify(r => r.AddAsync(It.Is<QuestStep>(s => s.SkillId == invalidSkillId), It.IsAny<CancellationToken>()), Times.Never);
    }
}