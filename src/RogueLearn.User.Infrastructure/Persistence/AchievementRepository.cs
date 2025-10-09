using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class AchievementRepository : GenericRepository<Achievement>, IAchievementRepository
{
  public AchievementRepository(Client supabaseClient) : base(supabaseClient)
  {
  }
}