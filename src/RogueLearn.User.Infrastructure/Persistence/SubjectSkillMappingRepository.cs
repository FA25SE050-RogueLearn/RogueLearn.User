// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/SubjectSkillMappingRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SubjectSkillMappingRepository : GenericRepository<SubjectSkillMapping>, ISubjectSkillMappingRepository
{
    public SubjectSkillMappingRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<SubjectSkillMapping>> GetMappingsBySubjectIdsAsync(IEnumerable<Guid> subjectIds, CancellationToken cancellationToken = default)
    {
        if (!subjectIds.Any())
        {
            return Enumerable.Empty<SubjectSkillMapping>();
        }

        var response = await _supabaseClient
            .From<SubjectSkillMapping>()
            .Filter("subject_id", Operator.In, subjectIds.Select(id => id.ToString()).ToList())
            .Get(cancellationToken);

        return response.Models;
    }
}
