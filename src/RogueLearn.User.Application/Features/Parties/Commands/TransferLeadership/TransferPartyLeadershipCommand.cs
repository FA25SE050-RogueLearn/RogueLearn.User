using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.TransferLeadership;

public record TransferPartyLeadershipCommand(Guid PartyId, Guid ToUserId) : IRequest<Unit>;