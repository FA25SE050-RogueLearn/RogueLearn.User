// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/SyllabusVersionRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SyllabusVersionRepository : GenericRepository<SyllabusVersion>, ISyllabusVersionRepository
{
    public SyllabusVersionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // MODIFIED: The previous single-ID method is replaced with an efficient, batch-capable method.
    /// <summary>
    /// Fetches all active syllabus versions for a given list of subject IDs in a single query.
    /// This implementation uses the native Supabase 'in' filter, which is more reliable than complex LINQ expressions.
    /// </summary>
    public async Task<IEnumerable<SyllabusVersion>> GetActiveBySubjectIdsAsync(List<Guid> subjectIds, CancellationToken cancellationToken = default)
    {
        if (subjectIds == null || !subjectIds.Any())
        {
            return Enumerable.Empty<SyllabusVersion>();
        }

        var response = await _supabaseClient
            .From<SyllabusVersion>()
            .Filter("subject_id", Operator.In, subjectIds.Select(id => id.ToString()).ToList())
            .Filter("is_active", Operator.Equals, "true")
            .Get(cancellationToken);

        return response.Models;
    }
}