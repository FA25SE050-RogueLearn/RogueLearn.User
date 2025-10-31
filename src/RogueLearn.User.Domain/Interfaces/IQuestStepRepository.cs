// RogueLearn.User/src/RogueLearn.User.Domain/Interfaces/IQuestStepRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

/// <summary>
/// Defines the contract for a repository that manages QuestStep entities.
/// Inherits generic repository operations from IGenericRepository.
/// </summary>
public interface IQuestStepRepository : IGenericRepository<QuestStep>
{
    // You can add quest-step-specific database operations here in the future if needed.
}