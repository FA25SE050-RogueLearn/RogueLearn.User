// RogueLearn.User/src/RogueLearn.User.Infrastructure/Persistence/StudentSemesterSubjectRepository.cs
using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase.Postgrest;
using Client = Supabase.Client;

namespace RogueLearn.User.Infrastructure.Persistence;

public class StudentSemesterSubjectRepository : GenericRepository<StudentSemesterSubject>, IStudentSemesterSubjectRepository
{
    public StudentSemesterSubjectRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Subject>> GetSubjectsByUserAsync(
        Guid authUserId,
        CancellationToken cancellationToken = default
        )
    {
        // FIX: Convert Guid to string for comparison
        var studentSemesterSubjects = await _supabaseClient
            .From<StudentSemesterSubject>()
            .Select("subject_id")
            .Filter("auth_user_id", Constants.Operator.Equals, authUserId.ToString())
            .Get(cancellationToken);

        var subjectIds = studentSemesterSubjects.Models.Select(e => e.SubjectId).Distinct().ToList();

        if (!subjectIds.Any())
            return new List<Subject>();

        // FIX: Convert List<Guid> to List<string> for the IN operator
        var subjectIdsString = subjectIds.Select(id => id.ToString()).ToList();

        // Then get the subjects
        var subjects = await _supabaseClient
            .From<Subject>()
            .Filter("id", Constants.Operator.In, subjectIdsString)
            .Get(cancellationToken);

        return subjects.Models;
    }
    // ADDED: Implementation for safe grade fetching
    public async Task<IEnumerable<StudentSemesterSubject>> GetSemesterSubjectsByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<StudentSemesterSubject>()
            .Filter("auth_user_id", Constants.Operator.Equals, authUserId.ToString())
            .Get(cancellationToken);

        return response.Models;
    }
}