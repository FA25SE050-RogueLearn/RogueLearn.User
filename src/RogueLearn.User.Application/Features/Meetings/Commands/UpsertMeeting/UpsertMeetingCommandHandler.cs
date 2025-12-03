using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Application.Interfaces;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertMeeting;

public class UpsertMeetingCommandHandler : IRequestHandler<UpsertMeetingCommand, MeetingDto>
{
    private readonly IMeetingRepository _meetingRepo;
    private readonly IMapper _mapper;
    private readonly IMeetingNotificationService? _meetingNotificationService;

    public UpsertMeetingCommandHandler(IMeetingRepository meetingRepo, IMapper mapper, IMeetingNotificationService meetingNotificationService)
    {
        _meetingRepo = meetingRepo;
        _mapper = mapper;
        _meetingNotificationService = meetingNotificationService;
    }

    public async Task<MeetingDto> Handle(UpsertMeetingCommand request, CancellationToken cancellationToken)
    {
        var dto = request.MeetingDto;
        if (dto.MeetingId == Guid.Empty)
        {
            var ownerId = dto.PartyId?.ToString() ?? dto.GuildId?.ToString() ?? string.Empty;
            dto.MeetingId = CreateDeterministicGuid(ownerId, dto.Title, dto.ScheduledStartTime.UtcDateTime.ToString("o"));
        }

        var entity = _mapper.Map<Meeting>(dto);
        entity.UpdatedAt = DateTimeOffset.UtcNow;
        if (await _meetingRepo.ExistsAsync(entity.MeetingId, cancellationToken))
        {
            await _meetingRepo.UpdateAsync(entity, cancellationToken);
        }
        else
        {
            entity.CreatedAt = DateTimeOffset.UtcNow;
            entity = await _meetingRepo.AddAsync(entity, cancellationToken);
            if (_meetingNotificationService != null)
            {
                await _meetingNotificationService.NotifyMeetingScheduledAsync(entity, cancellationToken);
            }
        }

        return _mapper.Map<MeetingDto>(entity);
    }

    private static Guid CreateDeterministicGuid(params string[] parts)
    {
        using var sha = SHA256.Create();
        var input = string.Join("|", parts.Select(p => p?.Trim() ?? string.Empty));
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}