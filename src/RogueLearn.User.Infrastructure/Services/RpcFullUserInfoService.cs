using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using RogueLearn.User.Application.Features.UserFullInfo.Queries.GetFullUserInfo;
using RogueLearn.User.Application.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Services;

public class RpcFullUserInfoService : IRpcFullUserInfoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly Client _client;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RpcFullUserInfoService(IHttpClientFactory httpClientFactory, IConfiguration configuration, Client client, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _client = client;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<FullUserInfoResponse?> GetAsync(Guid authUserId, int pageSize, int pageNumber, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, object>
        {
            ["p_auth_user_id"] = authUserId.ToString(),
            ["p_page_size"] = pageSize,
            ["p_page_number"] = pageNumber
        };

        var resp = await _client.Rpc("get_full_user_info", parameters);

        var json = resp.Content;
        if (string.IsNullOrWhiteSpace(json))
            return null;

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var dto = JsonSerializer.Deserialize<FullUserInfoResponse>(json, options);
        return dto;
    }
}