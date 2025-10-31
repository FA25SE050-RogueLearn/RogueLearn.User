using MediatR;
using RogueLearn.User.Domain.Enums;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetMemberRoles;

public record GetPartyMemberRolesQuery(Guid PartyId, Guid MemberAuthUserId) : IRequest<IReadOnlyList<PartyRole>>;