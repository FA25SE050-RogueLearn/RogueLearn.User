using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Client = Supabase.Client;

namespace RogueLearn.User.Infrastructure.Persistence;

public class SubjectRepository : GenericRepository<Subject>, ISubjectRepository
{
    public SubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
        
    }

    public async Task<IEnumerable<Subject>> GetSubjectsByRoute(Guid routeId, CancellationToken cancellationToken = default)
    {
        var routeSubjects= await _supabaseClient
            .From<CurriculumProgramSubject>()
            .Filter("program_id", Constants.Operator.Equals, routeId)
            .Get(cancellationToken);
        
        
        if (!routeSubjects.Models.Any())
            return new List<Subject>();

        var subjectIds = routeSubjects.Models.Select(cs => cs.SubjectId).ToList();

        // Fetch the actual subjects
        var subjects = await _supabaseClient
            .From<Subject>()
            .Filter("id", Constants.Operator.In, subjectIds)
            .Get(cancellationToken);

        return subjects.Models;
    }
}
