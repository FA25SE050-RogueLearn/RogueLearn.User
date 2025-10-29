// src/RogueLearn.User.Domain/Interfaces/IQuestRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface IQuestRepository : IGenericRepository<Quest>
{
}