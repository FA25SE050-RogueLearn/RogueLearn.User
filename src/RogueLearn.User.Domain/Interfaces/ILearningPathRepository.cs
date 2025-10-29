// src/RogueLearn.User.Domain/Interfaces/ILearningPathRepository.cs
using BuildingBlocks.Shared.Interfaces;
using RogueLearn.User.Domain.Entities;

namespace RogueLearn.User.Domain.Interfaces;

public interface ILearningPathRepository : IGenericRepository<LearningPath>
{
}