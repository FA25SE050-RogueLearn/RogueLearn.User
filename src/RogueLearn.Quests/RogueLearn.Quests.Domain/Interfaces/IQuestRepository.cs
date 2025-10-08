// RogueLearn.Quests.Domain/Interfaces/IQuestRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.Quests.Domain.Entities;

namespace RogueLearn.Quests.Domain.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
	// You can add quest-specific methods here later, for example:
	// Task<IEnumerable<Quest>> GetActiveQuestsBySubjectAsync(Guid subjectId);
}