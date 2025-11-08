using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class TagRepository : GenericRepository<Tag>, ITagRepository
{
  public TagRepository(Client supabaseClient) : base(supabaseClient)
  {
  }

  public async Task<IEnumerable<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
  {
    var idList = ids?.ToList() ?? new List<Guid>();
    if (!idList.Any())
      return Enumerable.Empty<Tag>();

    var response = await _supabaseClient
      .From<Tag>()
      .Filter("id", Operator.In, idList.Select(id => id.ToString()).ToList())
      .Get(cancellationToken);

    return response.Models;
  }
}