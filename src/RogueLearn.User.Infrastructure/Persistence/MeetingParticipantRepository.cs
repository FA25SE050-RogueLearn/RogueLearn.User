using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class MeetingParticipantRepository : IMeetingParticipantRepository
{
    private readonly Client _client;
    public MeetingParticipantRepository(Client client) { _client = client; }

    public async Task<bool> ExistsAsync(Guid participantId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<MeetingParticipant>()
            .Where(p => p.ParticipantId == participantId)
            .Get(cancellationToken);
        return resp.Models.Any();
    }

    public async Task<IEnumerable<MeetingParticipant>> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<MeetingParticipant>()
            .Where(p => p.MeetingId == meetingId)
            .Get(cancellationToken);
        return resp.Models;
    }

    public async Task<IEnumerable<MeetingParticipant>> GetByUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<MeetingParticipant>()
            .Where(p => p.UserId == authUserId)
            .Get(cancellationToken);
        return resp.Models;
    }

    public async Task<MeetingParticipant> AddAsync(MeetingParticipant entity, CancellationToken cancellationToken = default)
    {
        var resp = await _client.From<MeetingParticipant>().Insert(entity, cancellationToken: cancellationToken);
        return resp.Models.First();
    }

    public async Task UpdateAsync(MeetingParticipant entity, CancellationToken cancellationToken = default)
    {
        await _client.From<MeetingParticipant>().Update(entity, cancellationToken: cancellationToken);
    }
}