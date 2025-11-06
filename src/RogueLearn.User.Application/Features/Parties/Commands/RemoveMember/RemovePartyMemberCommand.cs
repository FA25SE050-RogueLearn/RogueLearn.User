using MediatR;

namespace RogueLearn.User.Application.Features.Parties.Commands.RemoveMember;

public record RemovePartyMemberCommand(Guid PartyId, Guid MemberId, string? Reason) : IRequest<Unit>;