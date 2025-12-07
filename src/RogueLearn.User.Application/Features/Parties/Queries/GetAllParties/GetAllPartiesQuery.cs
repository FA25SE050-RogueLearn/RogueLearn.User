using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;

public record GetAllPartiesQuery(bool IncludePrivate = true) : IRequest<IEnumerable<PartyDto>>;