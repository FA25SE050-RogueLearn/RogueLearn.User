using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using Supabase;

namespace RogueLearn.User.Infrastructure.Persistence;

public class MeetingSummaryRepository : IMeetingSummaryRepository
{
    private readonly Client _client;
    public MeetingSummaryRepository(Client client) { _client = client; }

    public async Task<MeetingSummary?> GetByMeetingAsync(Guid meetingId, CancellationToken cancellationToken = default)
    {
        var resp = await _client
            .From<MeetingSummary>()
            .Where(s => s.MeetingId == meetingId)
            .Get(cancellationToken);
        return resp.Models.FirstOrDefault();
    }

    public async Task<MeetingSummary> AddAsync(MeetingSummary entity, CancellationToken cancellationToken = default)
    {
        var resp = await _client.From<MeetingSummary>().Insert(entity, cancellationToken: cancellationToken);
        return resp.Models.First();
    }

    public async Task UpdateAsync(MeetingSummary entity, CancellationToken cancellationToken = default)
    {
        await _client.From<MeetingSummary>().Update(entity, cancellationToken: cancellationToken);
    }
}