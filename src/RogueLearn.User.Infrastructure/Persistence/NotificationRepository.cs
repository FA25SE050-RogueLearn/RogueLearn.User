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
}
