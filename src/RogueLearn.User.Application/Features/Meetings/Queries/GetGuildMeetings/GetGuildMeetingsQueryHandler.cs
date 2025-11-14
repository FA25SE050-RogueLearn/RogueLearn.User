using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetGuildMeetings;

public class GetGuildMeetingsQueryHandler : IRequestHandler<GetGuildMeetingsQuery, IReadOnlyList<MeetingDto>>
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMapper _mapper;

    public GetGuildMeetingsQueryHandler(IMeetingRepository meetingRepository, IMapper mapper)
    {
        _meetingRepository = meetingRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MeetingDto>> Handle(GetGuildMeetingsQuery request, CancellationToken cancellationToken)
    {
        var meetings = await _meetingRepository.GetByGuildAsync(request.GuildId, cancellationToken);
        return meetings.Select(m => _mapper.Map<MeetingDto>(m)).ToList();
    }
}