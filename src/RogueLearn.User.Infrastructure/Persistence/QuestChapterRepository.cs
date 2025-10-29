// src/RogueLearn.User.Infrastructure/Persistence/QuestChapterRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class QuestChapterRepository : GenericRepository<QuestChapter>, IQuestChapterRepository
{
    public QuestChapterRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}