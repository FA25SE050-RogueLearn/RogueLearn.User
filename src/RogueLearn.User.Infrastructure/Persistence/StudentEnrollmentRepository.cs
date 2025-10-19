using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Domain.Enums;
using Supabase;
using static Supabase.Postgrest.Constants;

namespace RogueLearn.User.Infrastructure.Persistence;

public class StudentEnrollmentRepository : GenericRepository<StudentEnrollment>, IStudentEnrollmentRepository
{
    public StudentEnrollmentRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<StudentEnrollment?> GetActiveForAuthUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var response = await _supabaseClient
            .From<StudentEnrollment>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("status", Operator.Equals, EnrollmentStatus.Active.ToString())
            .Single(cancellationToken);

        return response;
    }
}
