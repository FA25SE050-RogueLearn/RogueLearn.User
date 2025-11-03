// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/SyllabusVersionRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SyllabusVersionRepository : GenericRepository<SyllabusVersion>, ISyllabusVersionRepository
{
    public SyllabusVersionRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    // MODIFICATION: Implementing the new specialized method.
    /// <summary>
    /// Finds all active syllabus versions for a given subject.
    /// </summary>
    public async Task<IEnumerable<SyllabusVersion>> GetActiveBySubjectIdAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<SyllabusVersion>()
            .Filter("subject_id", Operator.Equals, subjectId.ToString())
            .Filter("is_active", Operator.Equals, "true")
            .Get(cancellationToken);

        return response.Models;
    }
}