using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyById;

public class GetPartyByIdQueryHandler : IRequestHandler<GetPartyByIdQuery, PartyDto?>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IMapper _mapper;

    public GetPartyByIdQueryHandler(IPartyRepository partyRepository, IMapper mapper)
    {
        _partyRepository = partyRepository;
        _mapper = mapper;
    }

    public async Task<PartyDto?> Handle(GetPartyByIdQuery request, CancellationToken cancellationToken)
    {
        var party = await _partyRepository.GetByIdAsync(request.PartyId, cancellationToken);
        return party is null ? null : _mapper.Map<PartyDto>(party);
    }
}