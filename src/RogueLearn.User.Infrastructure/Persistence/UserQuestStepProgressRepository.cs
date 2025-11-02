// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserQuestStepProgressRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestStepProgressRepository : GenericRepository<UserQuestStepProgress>, IUserQuestStepProgressRepository
{
    public UserQuestStepProgressRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}