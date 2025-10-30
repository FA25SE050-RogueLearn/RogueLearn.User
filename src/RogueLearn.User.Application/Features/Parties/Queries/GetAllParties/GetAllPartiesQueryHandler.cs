using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetAllParties;

public class GetAllPartiesQueryHandler : IRequestHandler<GetAllPartiesQuery, IReadOnlyList<PartyDto>>
{
    private readonly IPartyRepository _partyRepository;
    private readonly IMapper _mapper;

    public GetAllPartiesQueryHandler(IPartyRepository partyRepository, IMapper mapper)
    {
        _partyRepository = partyRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyDto>> Handle(GetAllPartiesQuery request, CancellationToken cancellationToken)
    {
        var parties = await _partyRepository.GetAllAsync(cancellationToken);
        return parties.Select(p => _mapper.Map<PartyDto>(p)).ToList();
    }
}