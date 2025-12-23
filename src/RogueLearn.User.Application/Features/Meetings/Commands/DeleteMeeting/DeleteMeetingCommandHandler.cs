using MediatR;
using RogueLearn.User.Application.Exceptions;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Commands.DeleteMeeting;

public class DeleteMeetingCommandHandler : IRequestHandler<DeleteMeetingCommand, Unit>
{
    private readonly IMeetingRepository _meetingRepo;
    private readonly IPartyMemberRepository _partyMemberRepo;
    private readonly IGuildMemberRepository _guildMemberRepo;

    public DeleteMeetingCommandHandler(
        IMeetingRepository meetingRepo,
        IPartyMemberRepository partyMemberRepo,
        IGuildMemberRepository guildMemberRepo)
    {
        _meetingRepo = meetingRepo;
        _partyMemberRepo = partyMemberRepo;
        _guildMemberRepo = guildMemberRepo;
    }

    public async Task<Unit> Handle(DeleteMeetingCommand request, CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepo.GetByIdAsync(request.MeetingId, cancellationToken);
        if (meeting == null)
        {
            throw new NotFoundException("Meeting", request.MeetingId.ToString());
        }

        bool hasPermission = false;

        if (meeting.PartyId.HasValue)
        {
            hasPermission = await _partyMemberRepo.IsLeaderAsync(meeting.PartyId.Value, request.RequestorId, cancellationToken);
        }
        else if (meeting.GuildId.HasValue)
        {
            hasPermission = await _guildMemberRepo.IsGuildMasterAsync(meeting.GuildId.Value, request.RequestorId, cancellationToken);
        }
        else
        {
            // Fallback to organizer if it's not a party or guild meeting
            hasPermission = meeting.OrganizerId == request.RequestorId;
        }

        if (!hasPermission)
        {
             throw new ForbiddenException("You do not have permission to delete this meeting.");
        }

        await _meetingRepo.DeleteAsync(meeting.MeetingId, cancellationToken);
        return Unit.Value;
    }
}
