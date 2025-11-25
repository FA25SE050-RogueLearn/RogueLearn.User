using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;
using static Supabase.Postgrest.Constants;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Infrastructure.Persistence;

public class LecturerVerificationRequestRepository : GenericRepository<LecturerVerificationRequest>, ILecturerVerificationRequestRepository
{
    public LecturerVerificationRequestRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<bool> AnyPendingAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<LecturerVerificationRequest>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("status", Operator.Equals, VerificationStatus.Pending.ToString())
            .Count(CountType.Exact, cancellationToken);
        return count > 0;
    }

    public async Task<bool> AnyApprovedAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<LecturerVerificationRequest>()
            .Filter("auth_user_id", Operator.Equals, authUserId.ToString())
            .Filter("status", Operator.Equals, VerificationStatus.Approved.ToString())
            .Count(CountType.Exact, cancellationToken);
        return count > 0;
    }
}
