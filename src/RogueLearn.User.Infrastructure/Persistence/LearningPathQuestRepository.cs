using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class LearningPathQuestRepository : GenericRepository<LearningPathQuest>, ILearningPathQuestRepository
{
    public LearningPathQuestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}