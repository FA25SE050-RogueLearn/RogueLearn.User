using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
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
        var studentSemesterSubjects = await _supabaseClient
            .From<StudentSemesterSubject>()
            .Select("subject_id")
            .Filter("auth_user_id", Constants.Operator.Equals, authUserId)
            .Get(cancellationToken);

        var subjectIds = studentSemesterSubjects.Models.Select(e => e.SubjectId).Distinct().ToList();

        if (!subjectIds.Any())
            return new List<Subject>();

        // Then get the subjects
        var subjects = await _supabaseClient
            .From<Subject>()
            .Filter("id", Constants.Operator.In, subjectIds)
            .Get(cancellationToken);

        return subjects.Models;
    }
}
