using MediatR;
using RogueLearn.User.Domain.Enums;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeclineJoinRequest;

public class DeclineGuildJoinRequestCommandHandler : IRequestHandler<DeclineGuildJoinRequestCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;
    private readonly IGuildJoinRequestRepository _joinRequestRepository;

    public DeclineGuildJoinRequestCommandHandler(IGuildRepository guildRepository, IGuildJoinRequestRepository joinRequestRepository)
    {
        _guildRepository = guildRepository;
        _joinRequestRepository = joinRequestRepository;
    }

    public async Task<Unit> Handle(DeclineGuildJoinRequestCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        var joinReq = await _joinRequestRepository.GetByIdAsync(request.RequestId, cancellationToken)
            ?? throw new Exceptions.NotFoundException("GuildJoinRequest", request.RequestId.ToString());

        if (joinReq.GuildId != request.GuildId)
        {
            throw new Exceptions.BadRequestException("Join request does not belong to target guild.");
        }

        if (joinReq.Status != GuildJoinRequestStatus.Pending || (joinReq.ExpiresAt.HasValue && joinReq.ExpiresAt <= DateTimeOffset.UtcNow))
        {
            throw new Exceptions.BadRequestException("Join request is not valid or already processed.");
        }

        joinReq.Status = GuildJoinRequestStatus.Declined;
        joinReq.RespondedAt = DateTimeOffset.UtcNow;
        await _joinRequestRepository.UpdateAsync(joinReq, cancellationToken);

        return Unit.Value;
    }
}