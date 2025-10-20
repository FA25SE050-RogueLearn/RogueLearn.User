using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Moq;
using RogueLearn.User.Application.Features.AiTagging.Commands.CommitNoteTagSelections;
using RogueLearn.User.Application.Features.AiTagging.Queries.SuggestNoteTags;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Application.Models;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace RogueLearn.User.Api.Tests.Controllers;

public class AiTaggingControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private readonly Mock<ITaggingSuggestionService> _mockSuggestionService = new();
    private readonly Mock<IFileTextExtractor> _mockFileTextExtractor = new();
    private readonly Mock<INoteRepository> _mockNoteRepository = new();
    private readonly Mock<ITagRepository> _mockTagRepository = new();
    private readonly Mock<INoteTagRepository> _mockNoteTagRepository = new();

    public AiTaggingControllerTests(WebApplicationFactory<Program> factory)
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
                ReplaceService(services, typeof(ITaggingSuggestionService), _mockSuggestionService.Object);
                ReplaceService(services, typeof(IFileTextExtractor), _mockFileTextExtractor.Object);
                ReplaceService(services, typeof(INoteRepository), _mockNoteRepository.Object);
                ReplaceService(services, typeof(ITagRepository), _mockTagRepository.Object);
                ReplaceService(services, typeof(INoteTagRepository), _mockNoteTagRepository.Object);

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
    public async Task Suggest_WithoutAuth_ShouldReturn401()
    {
        var json = JsonSerializer.Serialize(new { rawText = "Hello world" });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/api/ai/tagging/suggest", content);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Suggest_WithRawText_ShouldReturnSuggestions()
    {
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _mockSuggestionService
            .Setup(s => s.SuggestAsync(userId, "Hello world", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagSuggestionDto>
            {
                new TagSuggestionDto { Label = "Greeting", Confidence = 0.9 },
                new TagSuggestionDto { Label = "Intro", Confidence = 0.7 }
            });

        var json = JsonSerializer.Serialize(new { rawText = "Hello world", maxTags = 5 });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/ai/tagging/suggest", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SuggestNoteTagsResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.Suggestions.Should().HaveCount(2);
    }

    [Fact]
    public async Task SuggestUpload_WithFile_ShouldReturnSuggestions()
    {
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        _mockFileTextExtractor
            .Setup(e => e.ExtractTextAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("This is a test PDF content");

        _mockSuggestionService
            .Setup(s => s.SuggestAsync(userId, "This is a test PDF content", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TagSuggestionDto>
            {
                new TagSuggestionDto { Label = "PDF", Confidence = 0.8 }
            });

        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("dummy"));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(fileContent, name: "file", fileName: "test.pdf");
        form.Add(new StringContent("5"), name: "maxTags");

        var response = await _client.PostAsync("/api/ai/tagging/suggest-upload", form);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<SuggestNoteTagsResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.Suggestions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Commit_WithNewTags_ShouldReturnOk()
    {
        var userId = Guid.NewGuid();
        var token = GenerateJwtToken(userId, "User");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var noteId = Guid.NewGuid();
        _mockNoteRepository.Setup(r => r.GetByIdAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Note { Id = noteId, AuthUserId = userId, Content = "Sample" });

        _mockNoteTagRepository.Setup(r => r.GetTagIdsForNoteAsync(noteId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Guid>());

        var createdTagId = Guid.NewGuid();
        _mockTagRepository.Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Tag, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tag>());
        _mockTagRepository.Setup(r => r.AddAsync(It.IsAny<Tag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag t, CancellationToken _) => t);

        var cmd = new CommitNoteTagSelectionsCommand
        {
            NoteId = noteId,
            SelectedTagIds = new List<Guid>(),
            NewTagNames = new List<string> { "AI" },
        };
        var json = JsonSerializer.Serialize(cmd);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/api/ai/tagging/commit", content);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CommitNoteTagSelectionsResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.NoteId.Should().Be(noteId);
        result.TotalTagsAssigned.Should().Be(1);
    }

    private static void ReplaceService(IServiceCollection services, Type serviceType, object implementation)
    {
        var descriptor = services.SingleOrDefault(d => d.ServiceType == serviceType);
        if (descriptor != null)
            services.Remove(descriptor);
        services.AddScoped(serviceType, _ => implementation);
    }

    private static string GenerateJwtToken(Guid userId, string role)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes"));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

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
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}