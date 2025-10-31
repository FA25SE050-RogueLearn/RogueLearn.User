// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/QuestStepRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

/// <summary>
/// Implements the IQuestStepRepository for interacting with the "quest_steps" table
/// in the Supabase PostgreSQL database.
/// </summary>
public class QuestStepRepository : GenericRepository<QuestStep>, IQuestStepRepository
{
    public QuestStepRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}