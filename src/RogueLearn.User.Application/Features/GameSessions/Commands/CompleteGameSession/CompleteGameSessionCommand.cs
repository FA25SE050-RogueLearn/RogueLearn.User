using MediatR;

namespace RogueLearn.User.Application.Features.GameSessions.Commands.CompleteGameSession;

public sealed class CompleteGameSessionCommand : IRequest<CompleteGameSessionResponse>
{
    public Guid SessionId { get; }

    public CompleteGameSessionCommand(Guid sessionId)
    {
        SessionId = sessionId;
    }
}
