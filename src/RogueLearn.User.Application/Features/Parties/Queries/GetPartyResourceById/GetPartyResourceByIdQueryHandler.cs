using AutoMapper;
using MediatR;
using RogueLearn.User.Application.Features.Parties.DTOs;
using RogueLearn.User.Domain.Interfaces;

namespace RogueLearn.User.Application.Features.Parties.Queries.GetPartyResourceById;

public class GetPartyResourceByIdQueryHandler : IRequestHandler<GetPartyResourceByIdQuery, PartyStashItemDto?>
{
    private readonly IPartyStashItemRepository _stashRepository;
    private readonly IMapper _mapper;

    public GetPartyResourceByIdQueryHandler(IPartyStashItemRepository stashRepository, IMapper mapper)
    {
        _stashRepository = stashRepository;
        _mapper = mapper;
    }

    public async Task<PartyStashItemDto?> Handle(GetPartyResourceByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await _stashRepository.GetByIdAsync(request.StashItemId, cancellationToken);
        if (item is null || item.PartyId != request.PartyId) return null;
        return _mapper.Map<PartyStashItemDto>(item);
    }
}