// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/UserQuestAttemptRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class UserQuestAttemptRepository : GenericRepository<UserQuestAttempt>, IUserQuestAttemptRepository
{
    public UserQuestAttemptRepository(Client supabaseClient) : base(supabaseClient)
    {
    }
}