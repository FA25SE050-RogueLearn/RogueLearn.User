// src/RogueLearn.User.Infrastructure/Persistence/LearningPathRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class LearningPathRepository : GenericRepository<LearningPath>, ILearningPathRepository
{
    public LearningPathRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}