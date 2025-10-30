using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPendingInvitations;

public record GetPendingInvitationsQuery(Guid PartyId) : IRequest<IReadOnlyList<PartyInvitationDto>>;