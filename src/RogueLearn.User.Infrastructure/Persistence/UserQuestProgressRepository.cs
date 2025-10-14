using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestProgressRepository : GenericRepository<UserQuestProgress>, IUserQuestProgressRepository
{
    public UserQuestProgressRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}