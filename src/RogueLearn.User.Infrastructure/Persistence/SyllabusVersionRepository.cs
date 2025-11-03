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

    /// <summary>
    /// Finds all active syllabus versions for a given subject, ordered by version descending.
    /// This now correctly relies on the generic repository's handling of the updated entity model.
    /// </summary>
    public async Task<IEnumerable<SyllabusVersion>> GetActiveBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        // MODIFIED: The previous manual parsing was incorrect and causing deserialization issues.
        // By fixing the SyllabusVersion entity, we can now use the standard, reliable FindAsync method
        // from the generic repository. The Supabase client will handle the JSONB mapping automatically.
        var response = await _supabaseClient
            .From<SyllabusVersion>()
            .Filter("subject_id", Operator.Equals, subjectId.ToString())
            .Filter("is_active", Operator.Equals, "true")
            .Order("version_number", Ordering.Descending)
            .Get(cancellationToken);

        return response.Models;
    }
}