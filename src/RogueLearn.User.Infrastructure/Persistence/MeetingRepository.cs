using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class MeetingRepository : IMeetingRepository
{
    private readonly Client _client;
    public MeetingRepository(Client client) { _client = client; }

    public async Task<bool> ExistsAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<Meeting>()
            .Where(m => m.MeetingId == meetingId)
            .Get(cancellationToken);
        return resp.Models.Any();
    }

    public async Task<Meeting?> GetByIdAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<Meeting>()
            .Where(m => m.MeetingId == meetingId)
            .Get(cancellationToken);
        return resp.Models.FirstOrDefault();
    }

    public async Task<IEnumerable<Meeting>> GetByPartyAsync(Guid partyId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<Meeting>()
            .Where(m => m.PartyId == partyId)
            .Get(cancellationToken);
        return resp.Models;
    }

    public async Task<IEnumerable<Meeting>> GetByGuildAsync(Guid guildId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<Meeting>()
            .Where(m => m.GuildId == guildId)
            .Get(cancellationToken);
        return resp.Models;
    }

    public async Task<Meeting> AddAsync(Meeting entity, CancellationToken cancellationToken = default)
    {
        var resp = await _client.From<Meeting>().Insert(entity, cancellationToken: cancellationToken);
        return resp.Models.First();
    }

    public async Task UpdateAsync(Meeting entity, CancellationToken cancellationToken = default)
    {
        await _client.From<Meeting>().Update(entity, cancellationToken: cancellationToken);
    }
}