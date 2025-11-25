using MediatR;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Guilds.Commands.UpdateGuildMeritPoints;

public class UpdateGuildMeritPointsCommandHandler : IRequestHandler<UpdateGuildMeritPointsCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;

    public UpdateGuildMeritPointsCommandHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(UpdateGuildMeritPointsCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new Application.Exceptions.NotFoundException("Guild", request.GuildId.ToString());

        guild.MeritPoints += request.PointsDelta;
        await _guildRepository.UpdateAsync(guild, cancellationToken);

        return Unit.Value;
    }
}