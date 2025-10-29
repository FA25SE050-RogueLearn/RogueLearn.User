// src/RogueLearn.User.Infrastructure/Persistence/QuestRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class QuestRepository : GenericRepository<Quest>, IQuestRepository
{
    public QuestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}