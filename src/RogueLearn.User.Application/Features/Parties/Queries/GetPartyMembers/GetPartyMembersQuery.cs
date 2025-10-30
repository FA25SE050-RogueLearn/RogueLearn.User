using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyMembers;

public record GetPartyMembersQuery(Guid PartyId) : IRequest<IReadOnlyList<PartyMemberDto>>;