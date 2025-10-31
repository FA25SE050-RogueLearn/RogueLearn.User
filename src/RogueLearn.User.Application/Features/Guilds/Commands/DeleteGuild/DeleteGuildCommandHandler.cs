using MediatR;
using RogueLearn.User.Domain.Interfaces;
using RogueLearn.User.Application.Exceptions;

namespace RogueLearn.User.Application.Features.Guilds.Commands.DeleteGuild;

public class DeleteGuildCommandHandler : IRequestHandler<DeleteGuildCommand, Unit>
{
    private readonly IGuildRepository _guildRepository;

    public DeleteGuildCommandHandler(IGuildRepository guildRepository)
    {
        _guildRepository = guildRepository;
    }

    public async Task<Unit> Handle(DeleteGuildCommand request, CancellationToken cancellationToken)
    {
        var guild = await _guildRepository.GetByIdAsync(request.GuildId, cancellationToken)
            ?? throw new NotFoundException("Guild", request.GuildId.ToString());

        await _guildRepository.DeleteAsync(guild.Id, cancellationToken);
        return Unit.Value;
    }
}