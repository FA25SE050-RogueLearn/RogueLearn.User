using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyResources;

public class GetPartyResourcesQueryHandler : IRequestHandler<GetPartyResourcesQuery, IReadOnlyList<PartyStashItemDto>>
{
    private readonly IPartyStashItemRepository _stashRepository;
    private readonly IMapper _mapper;

    public GetPartyResourcesQueryHandler(IPartyStashItemRepository stashRepository, IMapper mapper)
    {
        _stashRepository = stashRepository;
        _mapper = mapper;
    }

    public async Task<IReadOnlyList<PartyStashItemDto>> Handle(GetPartyResourcesQuery request, CancellationToken cancellationToken)
    {
        var items = await _stashRepository.GetResourcesByPartyAsync(request.PartyId, cancellationToken);
        return items.Select(i => _mapper.Map<PartyStashItemDto>(i)).ToList();
    }
}