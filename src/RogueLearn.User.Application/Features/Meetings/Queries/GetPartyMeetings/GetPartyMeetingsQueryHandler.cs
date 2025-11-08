using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetPartyMeetings;

public class GetPartyMeetingsQueryHandler : IRequestHandler<GetPartyMeetingsQuery, IReadOnlyList<MeetingDto>>
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMapper _mapper;

    public GetPartyMeetingsQueryHandler(IMeetingRepository meetingRepository, IMapper mapper)
    {
        _meetingRepository = meetingRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MeetingDto>> Handle(GetPartyMeetingsQuery request, CancellationToken cancellationToken)
    {
        var meetings = await _meetingRepository.GetByPartyAsync(request.PartyId, cancellationToken);
        return meetings.Select(m => _mapper.Map<MeetingDto>(m)).ToList();
    }
}