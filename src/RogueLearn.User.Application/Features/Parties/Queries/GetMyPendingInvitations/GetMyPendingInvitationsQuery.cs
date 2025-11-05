using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMyPendingInvitations;

public record GetMyPendingInvitationsQuery(Guid AuthUserId) : IRequest<IReadOnlyList<PartyInvitationDto>>;