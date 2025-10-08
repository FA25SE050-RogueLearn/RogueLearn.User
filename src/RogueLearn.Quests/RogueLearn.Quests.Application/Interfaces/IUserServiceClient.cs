// RogueLearn.User/src/RogueLearn.Quests/RogueLearn.Quests.Application/Interfaces/IUserServiceClient.cs
using RogueLearn.Quests.Application.DTOs;

namespace RogueLearn.Quests.Application.Interfaces;

public interface IUserServiceClient
{
	Task<IEnumerable<CurriculumStructureDto>> GetCurriculumStructureAsync(Guid curriculumVersionId);
}