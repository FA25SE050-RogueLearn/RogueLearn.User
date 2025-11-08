using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Meetings.Queries.GetMeetingDetails;

public class GetMeetingDetailsQueryHandler : IRequestHandler<GetMeetingDetailsQuery, MeetingDetailsDto>
{
    private readonly IMeetingRepository _meetingRepository;
    private readonly IMeetingParticipantRepository _participantRepository;
    private readonly IMeetingSummaryRepository _summaryRepository;
    private readonly IMapper _mapper;

    public GetMeetingDetailsQueryHandler(IMeetingRepository meetingRepository, IMeetingParticipantRepository participantRepository, IMeetingSummaryRepository summaryRepository, IMapper mapper)
    {
        _meetingRepository = meetingRepository;
        _participantRepository = participantRepository;
        _summaryRepository = summaryRepository;
        _mapper = mapper;
    }

    public async Task<MeetingDetailsDto> Handle(GetMeetingDetailsQuery request, CancellationToken cancellationToken)
    {
        var meeting = await _meetingRepository.GetByIdAsync(request.MeetingId, cancellationToken) ?? throw new KeyNotFoundException("Meeting not found");
        var participants = await _participantRepository.GetByMeetingAsync(request.MeetingId, cancellationToken);
        var summary = await _summaryRepository.GetByMeetingAsync(request.MeetingId, cancellationToken);

        return new MeetingDetailsDto
        {
            Meeting = _mapper.Map<MeetingDto>(meeting),
            Participants = participants.Select(p => _mapper.Map<MeetingParticipantDto>(p)).ToList(),
            SummaryText = summary?.SummaryText
        };
    }
}