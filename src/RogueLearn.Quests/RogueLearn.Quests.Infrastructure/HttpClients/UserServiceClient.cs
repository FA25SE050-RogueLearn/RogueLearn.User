// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Infrastructure/HttpClients/UserServiceClient.cs
using System.Text.Json;
using RogueLearn.Quests.Application.DTOs;
using RogueLearn.Quests.Application.Interfaces;

namespace RogueLearn.Quests.Infrastructure.HttpClients;

public class UserServiceClient : IUserServiceClient
{
	private readonly HttpClient _httpClient;

	public UserServiceClient(HttpClient httpClient)
	{
		_httpClient = httpClient;
	}

	public async Task<IEnumerable<CurriculumStructureDto>> GetCurriculumStructureAsync(Guid curriculumVersionId)
	{
		// This endpoint path assumes the structure of the UserService API.
		// It may need to be adjusted to match Minh Anh's implementation.
		var response = await _httpClient.GetAsync($"api/admin/curriculum-versions/{curriculumVersionId}/subjects");

		response.EnsureSuccessStatusCode();

		var content = await response.Content.ReadAsStringAsync();
		var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
		var curriculumStructure = JsonSerializer.Deserialize<IEnumerable<CurriculumStructureDto>>(content, options);

		return curriculumStructure ?? new List<CurriculumStructureDto>();
	}
}