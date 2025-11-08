using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Meetings.DTOs;
using RogueLearn.User.Domain.Entities;
using RogueLearn.User.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace RogueLearn.User.Application.Features.Meetings.Commands.UpsertParticipants;

public class UpsertParticipantsCommandHandler : IRequestHandler<UpsertParticipantsCommand, IReadOnlyList<MeetingParticipantDto>>
{
    private readonly IMeetingParticipantRepository _repo;
    private readonly IMapper _mapper;

    public UpsertParticipantsCommandHandler(IMeetingParticipantRepository repo, IMapper mapper)
    {
        _repo = repo;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<MeetingParticipantDto>> Handle(UpsertParticipantsCommand request, CancellationToken cancellationToken)
    {
        var results = new List<MeetingParticipantDto>();
        foreach (var dto in request.Participants)
        {
            if (dto.ParticipantId == Guid.Empty)
            {
                dto.ParticipantId = CreateDeterministicGuid(request.MeetingId.ToString(), dto.UserId.ToString(), dto.RoleInMeeting);
            }
            dto.MeetingId = request.MeetingId;
            var entity = _mapper.Map<MeetingParticipant>(dto);
            if (await _repo.ExistsAsync(entity.ParticipantId, cancellationToken))
            {
                await _repo.UpdateAsync(entity, cancellationToken);
            }
            else
            {
                entity = await _repo.AddAsync(entity, cancellationToken);
            }
            results.Add(_mapper.Map<MeetingParticipantDto>(entity));
        }
        return results;
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