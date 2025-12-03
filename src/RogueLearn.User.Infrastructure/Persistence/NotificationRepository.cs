using BuildingBlocks.Shared.Repositories;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class NotificationRepository : GenericRepository<Notification>, INotificationRepository
{
    public NotificationRepository(Client supabaseClient) : base(supabaseClient)
    {
    }

    public async Task<IEnumerable<Notification>> GetLatestByUserAsync(Guid authUserId, int size = 20, CancellationToken cancellationToken = default)
    {
        var resp = await _supabaseClient
            .From<Notification>()
            .Filter("auth_user_id", Supabase.Postgrest.Constants.Operator.Equals, authUserId.ToString())
            .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(size)
            .Get(cancellationToken);
        return resp.Models;
    }

    public async Task<int> CountUnreadByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var count = await _supabaseClient
            .From<Notification>()
            .Filter("auth_user_id", Supabase.Postgrest.Constants.Operator.Equals, authUserId.ToString())
            .Filter("is_read", Supabase.Postgrest.Constants.Operator.Equals, "false")
            .Count(Supabase.Postgrest.Constants.CountType.Exact, cancellationToken);
        return (int)count;
    }

    public async Task<IEnumerable<Notification>> GetUnreadByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var resp = await _supabaseClient
            .From<Notification>()
            .Filter("auth_user_id", Supabase.Postgrest.Constants.Operator.Equals, authUserId.ToString())
            .Filter("is_read", Supabase.Postgrest.Constants.Operator.Equals, "false")
            .Get(cancellationToken);
        return resp.Models;
    }
}
