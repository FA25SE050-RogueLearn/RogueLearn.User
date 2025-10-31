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
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace RogueLearn.User.Api.Tests.Controllers;

public class PartiesControllerRoleTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly Mock<IPartyMemberRepository> _mockPartyMemberRepository;

    public PartiesControllerRoleTests(WebApplicationFactory<Program> factory)
    {
        _mockPartyMemberRepository = new Mock<IPartyMemberRepository>();

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
                var pmDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IPartyMemberRepository));
                if (pmDesc != null) services.Remove(pmDesc);

                services.AddScoped(_ => _mockPartyMemberRepository.Object);

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
    public async Task AssignPartyRole_AsLeader_ShouldReturnNoContent()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _mockPartyMemberRepository.Setup(x => x.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember m, CancellationToken _) => m);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignPartyRole_NotLeader_ShouldReturnForbidden()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssignPartyRole_CannotAssignLeader_ShouldReturnUnprocessableEntity()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var body = new { role = "Leader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task AssignPartyRole_Idempotent_ShouldReturnNoContent()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.CoLeader, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdminAssignPartyRole_WithGameMaster_ShouldReturnNoContent()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "Game Master" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        // Admin override bypasses leader check; only need target member existence
        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _mockPartyMemberRepository.Setup(x => x.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember m, CancellationToken _) => m);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/admin/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AdminAssignPartyRole_NonMember_ShouldReturnNotFound()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "Game Master" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember?)null);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/admin/parties/{partyId}/members/{memberId}/roles/assign", content);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokePartyRole_AsLeader_ShouldReturnNoContent()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.CoLeader, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);
        _mockPartyMemberRepository.Setup(x => x.UpdateAsync(It.IsAny<PartyMember>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember m, CancellationToken _) => m);

        var body = new { role = "CoLeader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/revoke", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RevokePartyRole_CannotRevokeLeader_ShouldReturnUnprocessableEntity()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Leader, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var body = new { role = "Leader" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/revoke", content);
        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task RevokePartyRole_CannotRevokeMember_ShouldReturnUnprocessableEntity()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var body = new { role = "Member" };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/revoke", content);
        response.StatusCode.Should().Be((HttpStatusCode)422);
    }

    [Fact]
    public async Task RevokePartyRole_Idempotent_ShouldReturnNoContent()
    {
        var actorAuthUserId = Guid.NewGuid();
        var token = GenerateJwtToken(actorAuthUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.IsLeaderAsync(partyId, actorAuthUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.Member, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var body = new { role = "CoLeader" }; // member does not have CoLeader, revoke is no-op
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _client.PostAsync($"/api/parties/{partyId}/members/{memberId}/roles/revoke", content);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPartyMemberRoles_ShouldReturnRolesList()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        var member = new PartyMember { PartyId = partyId, AuthUserId = memberId, Role = PartyRole.CoLeader, Status = MemberStatus.Active };
        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(member);

        var response = await _client.GetAsync($"/api/parties/{partyId}/members/{memberId}/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var roles = JsonSerializer.Deserialize<List<PartyRole>>(content, options);
        roles.Should().NotBeNull();
        roles!.Should().ContainSingle().Which.Should().Be(PartyRole.CoLeader);
    }

    [Fact]
    public async Task GetPartyMemberRoles_NonMember_ShouldReturnEmptyList()
    {
        var authUserId = Guid.NewGuid();
        var token = GenerateJwtToken(authUserId, roles: new[] { "User" });
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var partyId = Guid.NewGuid();
        var memberId = Guid.NewGuid();

        _mockPartyMemberRepository.Setup(x => x.GetMemberAsync(partyId, memberId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((PartyMember?)null);

        var response = await _client.GetAsync($"/api/parties/{partyId}/members/{memberId}/roles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
        var roles = JsonSerializer.Deserialize<List<PartyRole>>(content, options);
        roles.Should().NotBeNull();
        roles!.Should().BeEmpty();
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
            claims.Add(new Claim(ClaimTypes.Role, roles[0])); // include at least one ClaimTypes.Role
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