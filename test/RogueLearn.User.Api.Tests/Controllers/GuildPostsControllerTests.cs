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
using RogueLearn.User.Application.Features.GuildPosts.Commands.CreateGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.Commands.EditGuildPost;
using RogueLearn.User.Application.Features.GuildPosts.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class GuildPostsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IGuildPostRepository> _mockPostRepository;
    private readonly Mock<IGuildMemberRepository> _mockMemberRepository;
    private readonly Mock<IGuildRepository> _mockGuildRepository;

    public GuildPostsControllerTests(WebApplicationFactory<Program> factory)
    {
        _mockPostRepository = new Mock<IGuildPostRepository>();
        _mockMemberRepository = new Mock<IGuildMemberRepository>();
        _mockGuildRepository = new Mock<IGuildRepository>();

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
                // Replace repositories with mocks
                var postDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IGuildPostRepository));
                if (postDesc != null) services.Remove(postDesc);

                var memberDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IGuildMemberRepository));
                if (memberDesc != null) services.Remove(memberDesc);

                var guildDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IGuildRepository));
                if (guildDesc != null) services.Remove(guildDesc);

                services.AddScoped(_ => _mockPostRepository.Object);
                services.AddScoped(_ => _mockMemberRepository.Object);
                services.AddScoped(_ => _mockGuildRepository.Object);

                // Relax JWT validation and use local key
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
    public async Task GetGuildPosts_WithAuth_ShouldReturnOk()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var posts = new List<GuildPost>
        {
            new GuildPost { Id = Guid.NewGuid(), GuildId = guildId, AuthorId = authUserId, Title = "Title 1", Content = "Content 1", Tags = new[]{"tag1"}, Attachments = new Dictionary<string, object>{{"key","value"}}, Status = GuildPostStatus.published },
            new GuildPost { Id = Guid.NewGuid(), GuildId = guildId, AuthorId = authUserId, Title = "Title 2", Content = "Content 2", Tags = new[]{"tag2"}, Attachments = new Dictionary<string, object>{{"a",1}}, Status = GuildPostStatus.pending }
        };

        _mockPostRepository.Setup(x => x.GetByGuildAsync(guildId, null, null, null, null, 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(posts);

        var response = await _client.GetAsync($"/api/guilds/{guildId}/posts");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var result = JsonSerializer.Deserialize<IEnumerable<GuildPostDto>>(content, options);
        result.Should().NotBeNull();
        result!.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetGuildPosts_WithoutAuth_ShouldReturnUnauthorized()
    {
        var guildId = Guid.NewGuid();
        var response = await _client.GetAsync($"/api/guilds/{guildId}/posts");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateGuildPost_WithValidData_ShouldReturnCreated()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "Game Master" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        _mockGuildRepository.Setup(x => x.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, Name = "G1", RequiresApproval = false });

        _mockMemberRepository.Setup(x => x.GetMemberAsync(guildId, authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = authUserId, Status = MemberStatus.Active, Role = GuildRole.Member });

        _mockPostRepository.Setup(x => x.AddAsync(It.IsAny<GuildPost>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildPost p, CancellationToken _) => p);

        var req = new CreateGuildPostRequest
        {
            Title = "Hello",
            Content = "World",
            Tags = new[] { "intro" },
            Attachments = new Dictionary<string, object> { { "k", "v" } }
        };

        var json = JsonSerializer.Serialize(req);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/guilds/{guildId}/posts", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<CreateGuildPostResponse>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        result.Should().NotBeNull();
        result!.PostId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateGuildPost_NonMember_ShouldReturnForbidden()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        _mockGuildRepository.Setup(x => x.GetByIdAsync(guildId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Guild { Id = guildId, Name = "G1", RequiresApproval = false });

        _mockMemberRepository.Setup(x => x.GetMemberAsync(guildId, authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GuildMember?)null);

        var req = new CreateGuildPostRequest { Title = "T", Content = "C" };
        var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/guilds/{guildId}/posts", content);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EditGuildPost_AsAuthor_ShouldReturnNoContent()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authUserId, Title = "Old", Content = "Old", IsLocked = false };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockPostRepository.Setup(x => x.UpdateAsync(It.IsAny<GuildPost>(), It.IsAny<CancellationToken>())).ReturnsAsync((GuildPost p, CancellationToken _) => p);

        var req = new EditGuildPostRequest { Title = "New", Content = "New" };
        var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/guilds/{guildId}/posts/{postId}", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task EditGuildPost_NotAuthor_ShouldReturnForbidden()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = Guid.NewGuid(), Title = "Old", Content = "Old", IsLocked = false };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var req = new EditGuildPostRequest { Title = "New", Content = "New" };
        var content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json");
        var response = await _client.PutAsync($"/api/guilds/{guildId}/posts/{postId}", content);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteGuildPost_AsAuthor_ShouldReturnNoContent()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authUserId, IsLocked = false };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockPostRepository.Setup(x => x.DeleteAsync(postId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync($"/api/guilds/{guildId}/posts/{postId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteGuildPost_Locked_ShouldReturnForbidden()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = authUserId, IsLocked = true };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);

        var response = await _client.DeleteAsync($"/api/guilds/{guildId}/posts/{postId}");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_ForceDelete_WithPlatformAdmin_ShouldReturnNoContent()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "Game Master" }); // platform admin claim
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var post = new GuildPost { Id = postId, GuildId = guildId, AuthorId = Guid.NewGuid() };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockPostRepository.Setup(x => x.DeleteAsync(postId, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var response = await _client.DeleteAsync($"/api/admin/guilds/{guildId}/posts/{postId}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_Pin_WithOfficer_ShouldReturnNoContent()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "Game Master" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        // Attribute will check membership
        _mockMemberRepository.Setup(x => x.GetMemberAsync(guildId, authUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GuildMember { GuildId = guildId, AuthUserId = authUserId, Status = MemberStatus.Active, Role = GuildRole.Officer });

        var post = new GuildPost { Id = postId, GuildId = guildId };
        _mockPostRepository.Setup(x => x.GetByIdAsync(guildId, postId, It.IsAny<CancellationToken>())).ReturnsAsync(post);
        _mockPostRepository.Setup(x => x.UpdateAsync(It.IsAny<GuildPost>(), It.IsAny<CancellationToken>())).ReturnsAsync((GuildPost p, CancellationToken _) => p);

        var response = await _client.PostAsync($"/api/admin/guilds/{guildId}/posts/{postId}/pin", new StringContent(string.Empty, Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_Endpoints_WithoutAuth_ShouldReturnUnauthorized()
    {
        var guildId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var response = await _client.PostAsync($"/api/admin/guilds/{guildId}/posts/{postId}/pin", new StringContent(string.Empty));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private string GenerateJwtToken(Guid authUserId, string? role = null, string[]? roles = null)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("test-jwt-secret-that-is-at-least-256-bits-long-for-testing-purposes");

        var claims = new List<Claim>
        {
            new Claim("sub", authUserId.ToString()),
            new Claim("auth_user_id", authUserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, authUserId.ToString())
        };

        if (!string.IsNullOrEmpty(role))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        if (roles != null && roles.Length > 0)
        {
            // Include a comma-separated roles claim for attributes that check "roles"
            claims.Add(new Claim("roles", string.Join(',', roles)));
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
}