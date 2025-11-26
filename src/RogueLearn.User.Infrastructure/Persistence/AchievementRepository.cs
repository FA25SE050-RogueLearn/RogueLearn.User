using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class AchievementRepository : GenericRepository<Achievement>, IAchievementRepository
{
  public AchievementRepository(Client supabaseClient) : base(supabaseClient)
  {
  }

  public async Task<IEnumerable<Achievement>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
  {
    var list = ids.ToList();
    if (!list.Any()) return Enumerable.Empty<Achievement>();

    var response = await _supabaseClient
      .From<Achievement>()
      .Filter("id", Operator.In, list.Select(id => id.ToString()).ToList())
      .Get(cancellationToken);

    return response.Models;
  }
}