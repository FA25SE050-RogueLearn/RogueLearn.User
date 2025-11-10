// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/ClassSpecializationSubjectRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Client = Supabase.Client;

namespace RogueLearn.User.Infrastructure.Persistence;

public class ClassSpecializationSubjectRepository : GenericRepository<ClassSpecializationSubject>, IClassSpecializationSubjectRepository
{
    public ClassSpecializationSubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Subject>> GetSubjectByClassIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        // Get all subject IDs for this class
        var classSubjects = await _supabaseClient
          .From<ClassSpecializationSubject>()
          // FIX: The Guid must be converted to a string for the Supabase client's Filter method.
          .Filter("class_id", Constants.Operator.Equals, classId.ToString())
          .Get(cancellationToken);

        if (!classSubjects.Models.Any())
            return new List<Subject>();

        var subjectIds = classSubjects.Models.Select(cs => cs.SubjectId).ToList();

        // Fetch the actual subjects
        var subjects = await _supabaseClient
          .From<Subject>()
          .Filter("id", Constants.Operator.In, subjectIds)
          .Get(cancellationToken);

        return subjects.Models;
    }
}