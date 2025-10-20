using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ClassNodeRepository : GenericRepository<ClassNode>, IClassNodeRepository
{
  public ClassNodeRepository(Client supabaseClient) : base(supabaseClient)
  {
  }

  public async Task<IEnumerable<ClassNode>> GetByClassAndTitleAsync(Guid classId, string title, CancellationToken cancellationToken = default)
  {
    var response = await _supabaseClient
      .From<ClassNode>()
      .Filter("class_id", Operator.Equals, classId.ToString())
      .Filter("title", Operator.Equals, title)
      .Get(cancellationToken);

    return response.Models;
  }
}