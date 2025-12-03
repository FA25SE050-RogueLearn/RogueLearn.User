using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Infrastructure.Services;

public class MeetingNotificationService : IMeetingNotificationService
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IGuildMemberRepository _guildMemberRepository;
    private readonly IPartyMemberRepository _partyMemberRepository;
    private readonly IGuildRepository _guildRepository;
    private readonly IPartyRepository _partyRepository;
    private readonly IUserProfileRepository _userProfileRepository;

    public MeetingNotificationService(INotificationRepository notificationRepository, IGuildMemberRepository guildMemberRepository, IPartyMemberRepository partyMemberRepository, IGuildRepository guildRepository, IPartyRepository partyRepository, IUserProfileRepository userProfileRepository)
    {
        _notificationRepository = notificationRepository;
        _guildMemberRepository = guildMemberRepository;
        _partyMemberRepository = partyMemberRepository;
        _guildRepository = guildRepository;
        _partyRepository = partyRepository;
        _userProfileRepository = userProfileRepository;
    }

    public async Task NotifyMeetingScheduledAsync(Meeting meeting, CancellationToken cancellationToken = default)
    {
        if (meeting.PartyId.HasValue)
        {
            var partyName = (await _partyRepository.GetByIdAsync(meeting.PartyId.Value, cancellationToken))?.Name ?? string.Empty;
            var members = await _partyMemberRepository.GetMembersByPartyAsync(meeting.PartyId.Value, cancellationToken);
            foreach (var m in members.Where(x => x.Status == MemberStatus.Active))
            {
                var prof = await _userProfileRepository.GetByAuthIdAsync(m.AuthUserId, cancellationToken);
                var first = prof?.FirstName?.Trim();
                var last = prof?.LastName?.Trim();
                var both = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var memberName = string.IsNullOrWhiteSpace(both) ? (prof?.Username ?? string.Empty) : both;
                await _notificationRepository.AddAsync(new Notification
                {
                    AuthUserId = m.AuthUserId,
                    Type = NotificationType.Party,
                    Title = "Meeting scheduled",
                    Message = "A party meeting was scheduled.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["partyId"] = meeting.PartyId.Value,
                        ["meetingId"] = meeting.MeetingId,
                        ["partyName"] = partyName,
                        ["memberName"] = memberName
                    }
                }, cancellationToken);
            }
        }

        if (meeting.GuildId.HasValue)
        {
            var guildName = (await _guildRepository.GetByIdAsync(meeting.GuildId.Value, cancellationToken))?.Name ?? string.Empty;
            var members = await _guildMemberRepository.GetMembersByGuildAsync(meeting.GuildId.Value, cancellationToken);
            foreach (var m in members.Where(x => x.Status == MemberStatus.Active))
            {
                var prof = await _userProfileRepository.GetByAuthIdAsync(m.AuthUserId, cancellationToken);
                var first = prof?.FirstName?.Trim();
                var last = prof?.LastName?.Trim();
                var both = string.Join(" ", new[] { first, last }.Where(s => !string.IsNullOrWhiteSpace(s)));
                var memberName = string.IsNullOrWhiteSpace(both) ? (prof?.Username ?? string.Empty) : both;
                await _notificationRepository.AddAsync(new Notification
                {
                    AuthUserId = m.AuthUserId,
                    Type = NotificationType.Guild,
                    Title = "Meeting scheduled",
                    Message = "A guild meeting was scheduled.",
                    CreatedAt = DateTimeOffset.UtcNow,
                    Metadata = new Dictionary<string, object>
                    {
                        ["guildId"] = meeting.GuildId.Value,
                        ["meetingId"] = meeting.MeetingId,
                        ["guildName"] = guildName,
                        ["memberName"] = memberName
                    }
                }, cancellationToken);
            }
        }
    }
}