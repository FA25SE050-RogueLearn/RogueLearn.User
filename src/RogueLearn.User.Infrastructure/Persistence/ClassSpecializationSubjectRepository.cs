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

    /// <summary>
    /// This method performs the crucial lookup for specialized subjects.
    /// </summary>
    public async Task<IEnumerable<Subject>> GetSubjectByClassIdAsync(Guid classId, CancellationToken cancellationToken)
    {
        // STEP 1: Query the `class_specialization_subjects` join table to find all `subject_id`s
        // that are linked to the provided `classId`.
        var classSubjects = await _supabaseClient
          .From<ClassSpecializationSubject>()
          .Filter("class_id", Constants.Operator.Equals, classId.ToString())
          .Get(cancellationToken);

        if (!classSubjects.Models.Any())
        {
            // If there are no specialized subjects for this class, return an empty list.
            return new List<Subject>();
        }

        // STEP 2: Extract the list of unique subject IDs from the results of the first query.
        var subjectIds = classSubjects.Models.Select(cs => cs.SubjectId).ToList();

        // STEP 3: Query the main `subjects` table to fetch the full details for only those
        // subject IDs that were found in the previous step.
        var subjects = await _supabaseClient
          .From<Subject>()
          .Filter("id", Constants.Operator.In, subjectIds)
          .Get(cancellationToken);

        // STEP 4: Return the complete list of specialized Subject entities.
        return subjects.Models;
    }
}